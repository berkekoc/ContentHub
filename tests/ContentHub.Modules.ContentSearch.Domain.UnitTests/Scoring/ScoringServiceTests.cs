using ContentHub.Modules.ContentSearch.Domain.Model;
using ContentHub.Modules.ContentSearch.Domain.Scoring;
using Xunit;

namespace ContentHub.Modules.ContentSearch.Domain.UnitTests.Scoring;

public sealed class ScoringServiceTests
{
    // Sabit "şimdi" — güncellik sınır testleri için (S/Safety: C#↔SQL birebir).
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ComputePersistent_Video_UsesCaseFormulaExactly()
    {
        // base = 2000/1000 + 300/100 = 5 ; coef = 1.5 ; eng = (300/2000)*10 = 1.5
        // persistent = 5*1.5 + 1.5 = 9.0
        var metrics = ContentMetrics.ForVideo(views: 2000, likes: 300);

        var result = ScoringService.ComputePersistent(ContentType.Video, metrics);

        Assert.Equal(5.0m, result.BaseScore);
        Assert.Equal(1.5m, result.TypeCoefficient);
        Assert.Equal(1.5m, result.EngagementScore);
        Assert.Equal(9.0m, result.PersistentScore);
    }

    [Fact]
    public void ComputePersistent_Text_UsesCaseFormulaExactly()
    {
        // base = 10 + 100/50 = 12 ; coef = 1.0 ; eng = (100/10)*5 = 50
        // persistent = 12*1.0 + 50 = 62
        var metrics = ContentMetrics.ForText(readingTime: 10, reactions: 100);

        var result = ScoringService.ComputePersistent(ContentType.Text, metrics);

        Assert.Equal(12m, result.BaseScore);
        Assert.Equal(1.0m, result.TypeCoefficient);
        Assert.Equal(50m, result.EngagementScore);
        Assert.Equal(62m, result.PersistentScore);
    }

    [Fact]
    public void ComputePersistent_Video_ZeroViews_EngagementIsZero_NotDivideByZero()
    {
        // views=0 → etkileşim 0 (Norms 4). base = 0 + 50/100 = 0.5 ; persistent = 0.5*1.5 = 0.75
        var metrics = ContentMetrics.ForVideo(views: 0, likes: 50);

        var result = ScoringService.ComputePersistent(ContentType.Video, metrics);

        Assert.Equal(0m, result.EngagementScore);
        Assert.Equal(0.5m, result.BaseScore);
        Assert.Equal(0.75m, result.PersistentScore);
    }

    [Fact]
    public void ComputePersistent_Text_ZeroReadingTime_EngagementIsZero()
    {
        var metrics = ContentMetrics.ForText(readingTime: 0, reactions: 20);

        var result = ScoringService.ComputePersistent(ContentType.Text, metrics);

        Assert.Equal(0m, result.EngagementScore);
        Assert.Equal(0.4m, result.BaseScore);
        Assert.Equal(0.4m, result.PersistentScore);
    }

    [Fact]
    public void ComputePersistent_NullMetrics_TreatedAsZero()
    {
        var video = ScoringService.ComputePersistent(ContentType.Video, ContentMetrics.ForVideo(null, null));
        var text = ScoringService.ComputePersistent(ContentType.Text, ContentMetrics.ForText(null, null));

        Assert.Equal(0m, video.PersistentScore);
        Assert.Equal(0m, text.PersistentScore);
    }

    [Fact]
    public void ComputePersistent_NegativeMetrics_TreatedAsZero()
    {
        var metrics = ContentMetrics.ForVideo(views: -5, likes: -10);

        var result = ScoringService.ComputePersistent(ContentType.Video, metrics);

        Assert.Equal(0m, result.BaseScore);
        Assert.Equal(0m, result.EngagementScore);
        Assert.Equal(0m, result.PersistentScore);
    }

    public static TheoryData<DateTimeOffset, int> RecencyBoundaries() => new()
    {
        { Now, 5 },                              // bugün
        { Now.AddYears(1), 5 },                  // gelecek → hâlâ en taze
        { Now.AddDays(-7), 5 },                  // tam 1 hafta sınırı (dahil)
        { Now.AddDays(-7).AddSeconds(-1), 3 },   // 1 haftanın hemen ötesi
        { Now.AddMonths(-1), 3 },                // tam 1 ay sınırı (dahil)
        { Now.AddMonths(-1).AddSeconds(-1), 1 }, // 1 ayın hemen ötesi
        { Now.AddMonths(-3), 1 },                // tam 3 ay sınırı (dahil)
        { Now.AddMonths(-3).AddSeconds(-1), 0 }, // 3 ayın ötesi → +0
        { Now.AddYears(-1), 0 },                 // çok eski
    };

    [Theory]
    [MemberData(nameof(RecencyBoundaries))]
    public void RecencyPoints_RespectsCalendarBoundaries(DateTimeOffset publishedAt, int expected)
    {
        Assert.Equal(expected, ScoringService.RecencyPoints(publishedAt, Now));
    }

    [Fact]
    public void FinalScore_IsPersistentPlusRecency()
    {
        var metrics = ContentMetrics.ForVideo(views: 2000, likes: 300); // persistent 9.0
        var persistent = ScoringService.ComputePersistent(ContentType.Video, metrics).PersistentScore;

        var final = ScoringService.FinalScore(persistent, publishedAt: Now, now: Now);

        Assert.Equal(9.0m + 5, final); // güncellik +5 (bugün)
    }
}
