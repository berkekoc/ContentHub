using ContentHub.BuildingBlocks.Domain;

namespace ContentHub.Modules.ContentSearch.Domain.Scoring;

/// <summary>
/// Skorun ara bileşenleri — "skor neden bu?" açıklanabilirliğinin kaynağı.
/// <see cref="PersistentScore"/> çekimde saklanır; güncellik puanı burada YOKTUR,
/// okuma anında eklenir (S1, Norms 3).
/// </summary>
public sealed class ScoreComponents : ValueObject
{
    public ScoreComponents(
        decimal baseScore,
        decimal typeCoefficient,
        decimal engagementScore,
        decimal persistentScore)
    {
        BaseScore = baseScore;
        TypeCoefficient = typeCoefficient;
        EngagementScore = engagementScore;
        PersistentScore = persistentScore;
    }

    public decimal BaseScore { get; }

    public decimal TypeCoefficient { get; }

    public decimal EngagementScore { get; }

    /// <summary>(Temel × Katsayı) + Etkileşim. Kalıcı (uçucu olmayan) skor bileşeni.</summary>
    public decimal PersistentScore { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BaseScore;
        yield return TypeCoefficient;
        yield return EngagementScore;
        yield return PersistentScore;
    }
}
