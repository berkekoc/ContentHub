using ContentHub.BuildingBlocks.Domain;
using ContentHub.Modules.ContentSearch.Domain.Fingerprinting;
using ContentHub.Modules.ContentSearch.Domain.Scoring;

namespace ContentHub.Modules.ContentSearch.Domain.Model;

/// <summary>
/// Sistemin kanonik içerik nesnesi (sağlayıcı biçiminden bağımsız). Kalıcı skor ve
/// parmak izi türetilmiştir; ham ölçüt kaybolmadan saklanır (Norms 10). Doğal anahtar
/// (ProviderId, ExternalId) korunur; çekim idempotenttir (Norms 9).
/// </summary>
public sealed class ContentItem : AggregateRoot<Guid>
{
    private ContentItem()
    {
        // EF materializasyonu.
        ExternalId = null!;
        Title = null!;
        Fingerprint = null!;
        Metrics = null!;
        Score = null!;
    }

    private ContentItem(
        Guid id,
        Guid providerId,
        ExternalId externalId,
        string title,
        string? description,
        ContentType type,
        DateTimeOffset publishedAt,
        string? sourceUrl,
        ContentMetrics metrics,
        Fingerprint fingerprint,
        ContentScore score)
        : base(id)
    {
        ProviderId = providerId;
        ExternalId = externalId;
        Title = title;
        Description = description;
        Type = type;
        PublishedAt = publishedAt;
        SourceUrl = sourceUrl;
        Metrics = metrics;
        Fingerprint = fingerprint;
        Score = score;
    }

    public Guid ProviderId { get; private set; }

    public ExternalId ExternalId { get; private set; }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public ContentType Type { get; private set; }

    public DateTimeOffset PublishedAt { get; private set; }

    public string? SourceUrl { get; private set; }

    public Fingerprint Fingerprint { get; private set; }

    public ContentMetrics Metrics { get; private set; }

    public ContentScore Score { get; private set; }

    public static ContentItem Create(
        Guid providerId,
        ExternalId externalId,
        string title,
        string? description,
        ContentType type,
        DateTimeOffset publishedAt,
        string? sourceUrl,
        ContentMetrics metrics,
        DateTimeOffset computedAt)
    {
        Validate(providerId, title, metrics, type);

        var fingerprint = FingerprintFactory.Create(title, type, publishedAt, sourceUrl);
        var score = ContentScore.From(ScoringService.ComputePersistent(type, metrics), computedAt);

        return new ContentItem(
            Guid.CreateVersion7(),
            providerId,
            externalId,
            title.Trim(),
            description,
            type,
            publishedAt,
            sourceUrl,
            metrics,
            fingerprint,
            score);
    }

    /// <summary>
    /// Aynı doğal anahtar yeniden çekildiğinde çağrılır (idempotent upsert, Norms 9):
    /// değişebilir alanları günceller, parmak izi ve kalıcı skoru yeniden hesaplar.
    /// Kimlik ve doğal anahtar (ProviderId, ExternalId) DEĞİŞMEZ.
    /// </summary>
    public void UpdateFrom(
        string title,
        string? description,
        DateTimeOffset publishedAt,
        string? sourceUrl,
        ContentMetrics metrics,
        DateTimeOffset computedAt)
    {
        Validate(ProviderId, title, metrics, Type);

        Title = title.Trim();
        Description = description;
        PublishedAt = publishedAt;
        SourceUrl = sourceUrl;
        Metrics = metrics;
        Fingerprint = FingerprintFactory.Create(title, Type, publishedAt, sourceUrl);
        Score = ContentScore.From(ScoringService.ComputePersistent(Type, metrics), computedAt);
    }

    private static void Validate(Guid providerId, string title, ContentMetrics metrics, ContentType type)
    {
        if (providerId == Guid.Empty)
        {
            throw new DomainException("ContentItem bir sağlayıcıya ait olmalıdır.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("İçerik başlığı boş olamaz.");
        }

        ArgumentNullException.ThrowIfNull(metrics);
        metrics.EnsureValidFor(type);
    }
}
