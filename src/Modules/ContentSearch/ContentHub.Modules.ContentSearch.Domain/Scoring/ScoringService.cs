using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Domain.Scoring;

/// <summary>
/// Case puanlama formülünün saf (I/O'suz) uygulaması. Zaman DIŞARIDAN parametreyle
/// gelir; bu servis IClock'a, repository'e ya da HTTP'ye referans VEREMEZ (CLAUDE.md
/// kural 2, ArchTest kuralı 5). Her dal birim testle kilitlidir.
///
/// Nihai Skor = (Temel Puan × Tür Katsayısı) + Güncellik Puanı + Etkileşim Puanı
///   Temel   — Video: views/1000 + likes/100 ; Metin: readingTime + reactions/50
///   Katsayı — Video 1.5 ; Metin 1.0
///   Güncellik — ≤1 hafta +5 ; ≤1 ay +3 ; ≤3 ay +1 ; daha eski +0
///   Etkileşim — Video: (likes/views)*10 ; Metin: (reactions/readingTime)*5
///
/// Sıfıra bölme / eksik / negatif ölçüt → ilgili bileşen 0 (Norms 4, S5).
/// </summary>
public static class ScoringService
{
    private const decimal VideoCoefficient = 1.5m;
    private const decimal TextCoefficient = 1.0m;

    /// <summary>Kalıcı (zamandan bağımsız) bileşenleri hesaplar; çekimde saklanır.</summary>
    public static ScoreComponents ComputePersistent(ContentType type, ContentMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        decimal baseScore;
        decimal coefficient;
        decimal engagement;

        if (type == ContentType.Video)
        {
            var views = NonNegative(metrics.Views);
            var likes = NonNegative(metrics.Likes);

            baseScore = (views / 1000m) + (likes / 100m);
            coefficient = VideoCoefficient;
            engagement = views > 0m ? (likes / views) * 10m : 0m;
        }
        else
        {
            var readingTime = NonNegative(metrics.ReadingTime);
            var reactions = NonNegative(metrics.Reactions);

            baseScore = readingTime + (reactions / 50m);
            coefficient = TextCoefficient;
            engagement = readingTime > 0m ? (reactions / readingTime) * 5m : 0m;
        }

        var persistent = (baseScore * coefficient) + engagement;
        return new ScoreComponents(baseScore, coefficient, engagement, persistent);
    }

    /// <summary>
    /// Güncellik puanı — zamanın fonksiyonu (S1, Norms 3). SQL okuma tarafındaki
    /// CASE ifadesiyle BİREBİR aynı sınır semantiğini kullanır (takvim ayı, Notes 2):
    /// ≥ now-7g → 5 ; ≥ now-1ay → 3 ; ≥ now-3ay → 1 ; aksi → 0.
    /// </summary>
    public static int RecencyPoints(DateTimeOffset publishedAt, DateTimeOffset now)
        => (int)RecencyBandOf(publishedAt, now);

    public static RecencyBand RecencyBandOf(DateTimeOffset publishedAt, DateTimeOffset now)
    {
        if (publishedAt >= now.AddDays(-7))
        {
            return RecencyBand.Week;
        }

        if (publishedAt >= now.AddMonths(-1))
        {
            return RecencyBand.Month;
        }

        if (publishedAt >= now.AddMonths(-3))
        {
            return RecencyBand.Quarter;
        }

        return RecencyBand.Older;
    }

    /// <summary>Nihai skor = kalıcı bileşen + o anki güncellik puanı.</summary>
    public static decimal FinalScore(decimal persistentScore, DateTimeOffset publishedAt, DateTimeOffset now)
        => persistentScore + RecencyPoints(publishedAt, now);

    private static decimal NonNegative(long? value)
        => value is > 0 ? value.Value : 0m;

    private static decimal NonNegative(int? value)
        => value is > 0 ? value.Value : 0m;
}
