using System.Globalization;
using System.Text.Json;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Domain.Model;
using Microsoft.Extensions.Logging;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Providers;

/// <summary>
/// JSON sağlayıcı adaptörü (ACL) — WEG "provider1" sözleşmesi.
/// Zarf: { "contents": [ { id, title, type, metrics{ views, likes, duration }, published_at, tags[] } ], "pagination": {...} }
/// Geriye dönük tolerans: "items" zarfı, "publishedAt" ve "readingTime" alan adları da kabul edilir.
/// System.Text.Json YALNIZCA burada (Infrastructure/Providers) kullanılır — biçim domain'e sızmaz
/// (CLAUDE.md kuralı 1, ArchTest kuralı 4).
/// </summary>
internal sealed class JsonProviderAdapter : ProviderAdapterBase
{
    private readonly ILogger<JsonProviderAdapter> _logger;

    public JsonProviderAdapter(ProviderHttpClient client, ILogger<JsonProviderAdapter> logger)
        : base(client, logger)
        => _logger = logger;

    public override ProviderFormat Format => ProviderFormat.Json;

    protected override IReadOnlyList<FetchedContent> ParsePage(string payload, Provider provider)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        JsonElement items;
        if (!(root.TryGetProperty("contents", out items) && items.ValueKind == JsonValueKind.Array)
            && !(root.TryGetProperty("items", out items) && items.ValueKind == JsonValueKind.Array))
        {
            if (root.ValueKind == JsonValueKind.Array)
            {
                items = root;
            }
            else
            {
                return Array.Empty<FetchedContent>();
            }
        }

        var results = new List<FetchedContent>();
        foreach (var element in items.EnumerateArray())
        {
            try
            {
                results.Add(MapItem(element));
            }
            catch (Exception ex)
            {
                // Tek bozuk kayıt atlanır; çekim düşmez (S5 / uç durum dayanıklılığı).
                _logger.LogWarning(ex, "JSON kaydı atlandı: sağlayıcı={Provider}", provider.Name);
            }
        }

        return results;
    }

    private static FetchedContent MapItem(JsonElement element)
    {
        var externalId = GetString(element, "id") ?? throw new FormatException("id yok");
        var title = GetString(element, "title") ?? throw new FormatException("title yok");
        var description = GetString(element, "description");
        var type = ParseType(GetString(element, "type"));
        var publishedAt = ParseDate(GetString(element, "published_at") ?? GetString(element, "publishedAt"));
        var sourceUrl = GetString(element, "url") ?? GetString(element, "source_url");

        long? views = null, likes = null, reactions = null;
        int? readingTime = null;
        if (element.TryGetProperty("metrics", out var metrics) && metrics.ValueKind == JsonValueKind.Object)
        {
            if (type == ContentType.Video)
            {
                views = GetLong(metrics, "views");
                likes = GetLong(metrics, "likes");
            }
            else
            {
                readingTime = (int?)(GetLong(metrics, "reading_time") ?? GetLong(metrics, "readingTime"));
                reactions = GetLong(metrics, "reactions");
            }
        }

        return new FetchedContent(externalId, title, description, type, publishedAt, sourceUrl, views, likes, readingTime, reactions);
    }

    private static string? GetString(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static long? GetLong(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out var n) => n,
            JsonValueKind.String when long.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) => n,
            _ => null,
        };
    }

    private static ContentType ParseType(string? raw)
        => raw?.Trim().ToLowerInvariant() switch
        {
            "video" => ContentType.Video,
            "article" or "text" => ContentType.Text,
            _ => throw new FormatException($"Bilinmeyen tür: {raw}"),
        };

    private static DateTimeOffset ParseDate(string? raw)
        => DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var value)
            ? value
            : throw new FormatException($"Geçersiz tarih: {raw}");
}
