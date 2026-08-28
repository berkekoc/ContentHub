using System.Globalization;
using System.Xml.Linq;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Domain.Model;
using Microsoft.Extensions.Logging;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Providers;

/// <summary>
/// XML sağlayıcı adaptörü (ACL) — WEG "provider2" sözleşmesi.
/// <![CDATA[
///   <feed>
///     <meta><total_count/><current_page/><items_per_page/></meta>
///     <item>
///       <id/><headline/><type>video|article</type>
///       <stats> video: views,likes,duration | article: reading_time,reactions,comments </stats>
///       <publication_date>yyyy-MM-dd</publication_date>
///       <categories><category/></categories>
///     </item>
///   </feed>
/// ]]>
/// System.Xml.* YALNIZCA burada (Infrastructure/Providers) kullanılır — biçim domain'e sızmaz
/// (CLAUDE.md kuralı 1, ArchTest kuralı 4).
/// </summary>
internal sealed class XmlProviderAdapter : ProviderAdapterBase
{
    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd", "dd.MM.yyyy", "yyyy-MM-ddTHH:mm:ssZ", "o",
    };

    private readonly ILogger<XmlProviderAdapter> _logger;

    public XmlProviderAdapter(ProviderHttpClient client, ILogger<XmlProviderAdapter> logger)
        : base(client, logger)
        => _logger = logger;

    public override ProviderFormat Format => ProviderFormat.Xml;

    protected override IReadOnlyList<FetchedContent> ParsePage(string payload, Provider provider)
    {
        var root = XDocument.Parse(payload).Root;
        if (root is null)
        {
            return Array.Empty<FetchedContent>();
        }

        var results = new List<FetchedContent>();
        // <item> düğümleri feed altında; içeriğe konumdan bağımsız eriş.
        foreach (var node in root.Descendants("item"))
        {
            try
            {
                results.Add(MapItem(node));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "XML kaydı atlandı: sağlayıcı={Provider}", provider.Name);
            }
        }

        return results;
    }

    private static FetchedContent MapItem(XElement node)
    {
        // externalId hem <id> öğesi hem de eski 'externalId' niteliği olarak kabul edilir.
        var externalId = Value(node, "id")
            ?? (string?)node.Attribute("externalId")
            ?? throw new FormatException("id yok");
        var type = ParseType(Value(node, "type") ?? (string?)node.Attribute("kind"));
        var title = Value(node, "headline") ?? Value(node, "title") ?? throw new FormatException("headline yok");
        var description = Value(node, "summary") ?? Value(node, "description");
        var publishedAt = ParseDate(Value(node, "publication_date") ?? Value(node, "released"));
        var sourceUrl = Value(node, "link") ?? Value(node, "url");

        var stats = node.Element("stats");
        long? views = null, likes = null, reactions = null;
        int? readingTime = null;
        if (stats is not null)
        {
            if (type == ContentType.Video)
            {
                views = ParseLong(stats.Element("views") ?? stats.Element("viewCount"));
                likes = ParseLong(stats.Element("likes") ?? stats.Element("likeCount"));
            }
            else
            {
                readingTime = (int?)ParseLong(stats.Element("reading_time") ?? stats.Element("minutes"));
                reactions = ParseLong(stats.Element("reactions") ?? stats.Element("reactionCount"));
            }
        }

        return new FetchedContent(externalId, title, description, type, publishedAt, sourceUrl, views, likes, readingTime, reactions);
    }

    private static string? Value(XElement parent, string name)
    {
        var element = parent.Element(name);
        return element is null ? null : element.Value?.Trim();
    }

    private static ContentType ParseType(string? raw)
        => raw?.Trim().ToLowerInvariant() switch
        {
            "video" => ContentType.Video,
            "article" or "text" => ContentType.Text,
            _ => throw new FormatException($"Bilinmeyen tür: {raw}"),
        };

    private static long? ParseLong(XElement? element)
        => element is not null && long.TryParse(element.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static DateTimeOffset ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new FormatException("publication_date yok");
        }

        raw = raw.Trim();
        if (DateTimeOffset.TryParseExact(raw, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var exact))
        {
            return exact;
        }

        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var loose))
        {
            return loose;
        }

        throw new FormatException($"Geçersiz tarih: {raw}");
    }
}
