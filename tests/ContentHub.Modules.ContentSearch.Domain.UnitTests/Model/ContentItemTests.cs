using ContentHub.BuildingBlocks.Domain;
using ContentHub.Modules.ContentSearch.Domain.Model;
using Xunit;

namespace ContentHub.Modules.ContentSearch.Domain.UnitTests.Model;

public sealed class ContentItemTests
{
    private static readonly Guid ProviderId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Published = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ComputedAt = new(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ComputesFingerprintAndPersistentScore()
    {
        var item = ContentItem.Create(
            ProviderId,
            ExternalId.Create("ext-1"),
            "Örnek Video",
            "açıklama",
            ContentType.Video,
            Published,
            sourceUrl: null,
            ContentMetrics.ForVideo(2000, 300),
            ComputedAt);

        Assert.NotNull(item.Fingerprint);
        Assert.Equal(9.0m, item.Score.PersistentScore);
        Assert.Equal(ComputedAt, item.Score.ComputedAt);
    }

    [Fact]
    public void Create_WithMismatchedMetrics_Throws()
    {
        // Video içeriğe metin ölçütü verilemez (Norms 1).
        var badMetrics = ContentMetrics.Rehydrate(views: 1, likes: 1, readingTime: 5, reactions: 5);

        Assert.Throws<DomainException>(() => ContentItem.Create(
            ProviderId,
            ExternalId.Create("ext-2"),
            "Kötü",
            null,
            ContentType.Video,
            Published,
            null,
            badMetrics,
            ComputedAt));
    }

    [Fact]
    public void UpdateFrom_RecomputesScoreAndFingerprint_KeepsIdentity()
    {
        var item = ContentItem.Create(
            ProviderId,
            ExternalId.Create("ext-3"),
            "İlk",
            null,
            ContentType.Text,
            Published,
            null,
            ContentMetrics.ForText(10, 100),
            ComputedAt);

        var originalId = item.Id;
        var originalFingerprint = item.Fingerprint;

        item.UpdateFrom(
            "İkinci Başlık",
            "yeni",
            Published.AddDays(2),
            null,
            ContentMetrics.ForText(20, 200),
            ComputedAt.AddDays(1));

        Assert.Equal(originalId, item.Id);                 // kimlik değişmez (idempotency)
        Assert.NotEqual(originalFingerprint, item.Fingerprint); // başlık/tarih değişti
        Assert.Equal("İkinci Başlık", item.Title);
        // base = 20 + 200/50 = 24 ; eng = (200/20)*5 = 50 ; persistent = 24 + 50 = 74
        Assert.Equal(74m, item.Score.PersistentScore);
    }
}
