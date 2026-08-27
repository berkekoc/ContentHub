using ContentHub.Modules.ContentSearch.Domain.Scoring;

namespace ContentHub.Modules.ContentSearch.Domain.Model;

/// <summary>
/// İçeriğin SAKLANAN skor bileşenleri. Güncellik puanı burada yoktur — okuma anında
/// eklenir (S1, Norms 3). ContentItem'ın sahip olduğu (owned) 1—1 parçadır.
/// </summary>
public sealed class ContentScore
{
    private ContentScore()
    {
    }

    private ContentScore(
        decimal baseScore,
        decimal typeCoefficient,
        decimal engagementScore,
        decimal persistentScore,
        DateTimeOffset computedAt)
    {
        BaseScore = baseScore;
        TypeCoefficient = typeCoefficient;
        EngagementScore = engagementScore;
        PersistentScore = persistentScore;
        ComputedAt = computedAt;
    }

    public decimal BaseScore { get; private set; }

    public decimal TypeCoefficient { get; private set; }

    public decimal EngagementScore { get; private set; }

    public decimal PersistentScore { get; private set; }

    public DateTimeOffset ComputedAt { get; private set; }

    public static ContentScore From(ScoreComponents components, DateTimeOffset computedAt)
    {
        ArgumentNullException.ThrowIfNull(components);
        return new ContentScore(
            components.BaseScore,
            components.TypeCoefficient,
            components.EngagementScore,
            components.PersistentScore,
            computedAt);
    }
}
