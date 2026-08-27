using System.Globalization;
using System.Xml.Linq;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Domain.Model;
using Microsoft.Extensions.Logging;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Providers;

/// <summary>
/// XML sağlayıcı adaptörü (ACL). Bilinçle zorlaştırılmış şema: nitelikler (externalId, kind),
/// iç içe &lt;stats&gt;, farklı alan adları ve farklı tarih biçimleri (dd.MM.yyyy | yyyy-MM-dd).
/// System.Xml.* yalnızca burada (Infrastructure/Providers) — biçim domain'e sızmaz.
/// </summary>
internal sealed class XmlProviderAdapter : ProviderAdapterBase
{
    private static readonly string[] DateFormats =
    {
        "dd.MM.yyyy", "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ssZ", "o",
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
        foreach (var node in root.Elements("content"))
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
        var externalId = (string?)node.Attribute("externalId") ?? throw new FormatException("externalId yok");
        var type = ParseKind((string?)node.Attribute("kind"));
        var title = (string?)node.Element("heading") ?? throw new FormatException("heading yok");
        var description = (string?)node.Element("summary");
        var publishedAt = ParseDate((string?)node.Element("released"));
        var sourceUrl = (string?)node.Element("link");

        var stats = node.Element("stats");
        long? views = null, likes = null, reactions = null;
        int? readingTime = null;
        if (stats is not null)
        {
            if (type == ContentType.Video)
            {
                views = ParseLong(stats.Element("viewCount"));
                likes = ParseLong(stats.Element("likeCount"));
            }
            else
            {
                readingTime = (int?)ParseLong(stats.Element("minutes"));
                reactions = ParseLong(stats.Element("reactionCount"));
            }
        }

        return new FetchedContent(externalId, title, description, type, publishedAt, sourceUrl, views, likes, readingTime, reactions);
    }

    private static ContentType ParseKind(string? raw)
        => raw?.Trim().ToLowerInvariant() switch
        {
            "video" => ContentType.Video,
            "text" => ContentType.Text,
            _ => throw new FormatException($"Bilinmeyen kind: {raw}"),
        };

    private static long? ParseLong(XElement? element)
        => element is not null && long.TryParse(element.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private static DateTimeOffset ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new FormatException("released yok");
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
