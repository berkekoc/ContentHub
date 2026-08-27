using ContentHub.BuildingBlocks.Domain;
using ContentHub.Modules.ContentSearch.Domain.Model;
using Xunit;

namespace ContentHub.Modules.ContentSearch.Domain.UnitTests.Model;

public sealed class ContentMetricsTests
{
    [Fact]
    public void EnsureValidFor_VideoWithTextMetrics_Throws()
    {
        var metrics = ContentMetrics.Rehydrate(null, null, readingTime: 5, reactions: 3);
        Assert.Throws<DomainException>(() => metrics.EnsureValidFor(ContentType.Video));
    }

    [Fact]
    public void EnsureValidFor_TextWithVideoMetrics_Throws()
    {
        var metrics = ContentMetrics.Rehydrate(views: 10, likes: 2, null, null);
        Assert.Throws<DomainException>(() => metrics.EnsureValidFor(ContentType.Text));
    }

    [Fact]
    public void EnsureValidFor_MatchingType_DoesNotThrow()
    {
        ContentMetrics.ForVideo(10, 2).EnsureValidFor(ContentType.Video);
        ContentMetrics.ForText(10, 2).EnsureValidFor(ContentType.Text);
    }
}
