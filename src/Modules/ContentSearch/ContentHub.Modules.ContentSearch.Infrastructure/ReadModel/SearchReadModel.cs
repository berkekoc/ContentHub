using System.Data;
using System.Linq;
using ContentHub.BuildingBlocks.Application.Models;
using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Domain.Model;
using ContentHub.Modules.ContentSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace ContentHub.Modules.ContentSearch.Infrastructure.ReadModel;

/// <summary>
/// CQRS okuma projeksiyonu — parametreli ham SQL, no-tracking. Yazma modelini (aggregate)
/// KULLANMAZ. Güncellik CASE'i C#'taki ScoringService.RecencyPoints ile BİREBİR aynı sınır
/// semantiğini taşır (S/Safety sınır testi). Dedup: DISTINCT ON(fingerprint), temsilci = en
/// yüksek final_score; provider_count = grup büyüklüğü. Sıralama kararlıdır (ikincil id ASC).
/// </summary>
internal sealed class SearchReadModel : ISearchReadModel
{
    private readonly ContentSearchDbContext _db;
    private readonly SearchReadOptions _options;

    public SearchReadModel(ContentSearchDbContext db, IOptions<SearchReadOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<PagedResult<ContentItemDto>> SearchAsync(
        SearchCriteria criteria,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var config = _options.TextSearchConfig;

        // matched: filtre + okuma-anı final_score + relevance. keyword null → tümü.
        var matchedCte =
            $@"matched AS (
                SELECT ci.id, ci.provider_id, ci.title, ci.description, ci.content_type, ci.published_at, ci.fingerprint,
                       cs.persistent_score
                         + CASE
                             WHEN ci.published_at >= @now - interval '7 days'  THEN 5
                             WHEN ci.published_at >= @now - interval '1 month' THEN 3
                             WHEN ci.published_at >= @now - interval '3 months' THEN 1
                             ELSE 0
                           END AS final_score,
                       CASE WHEN @tsquery IS NULL THEN 0
                            ELSE ts_rank(ci.search_vector, to_tsquery('{config}', @tsquery), 1)  -- 1 = uzunluk normalizasyonu: kısa/yoğun başlık üste
                       END AS relevance
                FROM content_search.content_items ci
                JOIN content_search.content_scores cs ON cs.content_item_id = ci.id
                WHERE (@tsquery IS NULL OR ci.search_vector @@ to_tsquery('{config}', @tsquery))
                  AND (@contentType IS NULL OR ci.content_type = @contentType)
            )";

        var orderBy = criteria.Sort switch
        {
            SortOption.Relevance => "r.relevance DESC, r.id ASC",
            SortOption.Hybrid => "(@wRel * r.relevance + @wPop * (r.final_score / @scale)) DESC, r.id ASC",
            _ => "r.final_score DESC, r.id ASC", // Popularity
        };

        var offset = (criteria.Page - 1) * criteria.PageSize;

        var pageSql =
            $@"WITH {matchedCte},
            groups AS (
                SELECT fingerprint, COUNT(DISTINCT provider_id) AS provider_count FROM matched GROUP BY fingerprint
            ),
            reps AS (
                SELECT DISTINCT ON (m.fingerprint)
                       m.id, m.title, m.description, m.content_type, m.published_at, m.final_score, m.relevance, m.fingerprint
                FROM matched m
                ORDER BY m.fingerprint, m.final_score DESC, m.id ASC
            )
            SELECT r.id, r.title, r.description, r.content_type, r.published_at, r.final_score, r.relevance, g.provider_count
            FROM reps r
            JOIN groups g ON g.fingerprint = r.fingerprint
            ORDER BY {orderBy}
            OFFSET @offset LIMIT @limit;";

        var countSql = $@"WITH {matchedCte} SELECT COUNT(DISTINCT fingerprint) FROM matched;";

        var connection = (NpgsqlConnection)_db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            long total;
            await using (var countCommand = new NpgsqlCommand(countSql, connection))
            {
                AddFilterParameters(countCommand, criteria, now);
                var scalar = await countCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                total = scalar is long l ? l : Convert.ToInt64(scalar);
            }

            if (total == 0)
            {
                return PagedResult<ContentItemDto>.Empty(criteria.Page, criteria.PageSize);
            }

            var items = new List<ContentItemDto>(criteria.PageSize);
            await using (var pageCommand = new NpgsqlCommand(pageSql, connection))
            {
                AddFilterParameters(pageCommand, criteria, now);
                pageCommand.Parameters.Add(new NpgsqlParameter("offset", NpgsqlDbType.Integer) { Value = offset });
                pageCommand.Parameters.Add(new NpgsqlParameter("limit", NpgsqlDbType.Integer) { Value = criteria.PageSize });
                pageCommand.Parameters.Add(new NpgsqlParameter("wRel", NpgsqlDbType.Double) { Value = _options.HybridRelevanceWeight });
                pageCommand.Parameters.Add(new NpgsqlParameter("wPop", NpgsqlDbType.Double) { Value = _options.HybridPopularityWeight });
                pageCommand.Parameters.Add(new NpgsqlParameter("scale", NpgsqlDbType.Double) { Value = _options.HybridScale });

                await using var reader = await pageCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    items.Add(new ContentItemDto(
                        Id: reader.GetGuid(0),
                        Title: reader.GetString(1),
                        Description: await reader.IsDBNullAsync(2, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(2),
                        Type: (ContentType)reader.GetInt16(3),
                        PublishedAt: reader.GetFieldValue<DateTimeOffset>(4),
                        FinalScore: reader.GetDecimal(5),
                        Relevance: Convert.ToDouble(reader.GetValue(6)),
                        ProviderCount: (int)reader.GetInt64(7)));
                }
            }

            return new PagedResult<ContentItemDto>(items, criteria.Page, criteria.PageSize, total);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Anahtar kelimeyi PREFIX (önek) tsquery'sine çevirir: her terim sanitize edilip ':*' ile
    /// aranır (ör. "clea" → clean, "API" → apis). Böylece kısmi/yazarken arama boş dönmez.
    /// Yalnız harf/rakam korunur; tsquery meta-karakteri sızmaz (güvenli). Terim yoksa null → tümü.
    /// </summary>
    private static string? BuildPrefixTsQuery(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return null;
        }

        var terms = new List<string>();
        foreach (var raw in keyword.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var cleaned = new string(raw.Where(char.IsLetterOrDigit).ToArray());
            if (cleaned.Length > 0)
            {
                terms.Add(cleaned.ToLowerInvariant() + ":*");
            }
        }

        return terms.Count == 0 ? null : string.Join(" & ", terms);
    }

    private static void AddFilterParameters(NpgsqlCommand command, SearchCriteria criteria, DateTimeOffset now)
    {
        command.Parameters.Add(new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now });
        command.Parameters.Add(new NpgsqlParameter("tsquery", NpgsqlDbType.Text)
        {
            Value = (object?)BuildPrefixTsQuery(criteria.Keyword) ?? DBNull.Value,
        });
        command.Parameters.Add(new NpgsqlParameter("contentType", NpgsqlDbType.Smallint)
        {
            Value = criteria.ContentType is { } type ? (short)type : DBNull.Value,
        });
    }
}
