using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Application.Contracts;

/// <summary>
/// Adaptörün ürettiği KANONİK içerik kaydı (ACL çıktısı). Sağlayıcı biçimi (JSON/XML)
/// bu noktadan sonra görünmez. Tür-dışı ölçüt alanları null gelir; eşleme handler'da yapılır.
/// </summary>
public sealed record FetchedContent(
    string ExternalId,
    string Title,
    string? Description,
    ContentType Type,
    DateTimeOffset PublishedAt,
    string? SourceUrl,
    long? Views,
    long? Likes,
    int? ReadingTime,
    long? Reactions)
{
    public ContentMetrics ToMetrics()
        => Type == ContentType.Video
            ? ContentMetrics.ForVideo(Views, Likes)
            : ContentMetrics.ForText(ReadingTime, Reactions);
}
