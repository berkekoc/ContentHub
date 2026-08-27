using ContentHub.BuildingBlocks.Domain;

namespace ContentHub.Modules.ContentSearch.Domain.Model;

/// <summary>
/// İçeriğin ham sayaçları — türe göre farklı set (Norms 1). Video: views, likes.
/// Metin: readingTime, reactions. Ham veri atılmaz (Norms 10); tür-dışı alanlar null'dır.
/// Değerler nullable; eksik/negatif ölçüt puanlamada 0 kabul edilir (Norms 4, ScoringService).
/// </summary>
public sealed class ContentMetrics : ValueObject
{
    private ContentMetrics(long? views, long? likes, int? readingTime, long? reactions)
    {
        Views = views;
        Likes = likes;
        ReadingTime = readingTime;
        Reactions = reactions;
    }

    public long? Views { get; }

    public long? Likes { get; }

    public int? ReadingTime { get; }

    public long? Reactions { get; }

    /// <summary>Video ölçütleri; metin alanları anlamsız olduğu için null bırakılır.</summary>
    public static ContentMetrics ForVideo(long? views, long? likes)
        => new(views, likes, readingTime: null, reactions: null);

    /// <summary>Metin ölçütleri; video alanları anlamsız olduğu için null bırakılır.</summary>
    public static ContentMetrics ForText(int? readingTime, long? reactions)
        => new(views: null, likes: null, readingTime, reactions);

    /// <summary>Kalıcılıktan yeniden kurma (EF); tür geçerliliği ContentItem'da korunur.</summary>
    public static ContentMetrics Rehydrate(long? views, long? likes, int? readingTime, long? reactions)
        => new(views, likes, readingTime, reactions);

    /// <summary>Türün ölçüt setine uyulup uyulmadığını doğrular (Norms 1).</summary>
    public void EnsureValidFor(ContentType type)
    {
        switch (type)
        {
            case ContentType.Video when ReadingTime is not null || Reactions is not null:
                throw new DomainException("Video içeriğine metin ölçütü (readingTime/reactions) yazılamaz.");
            case ContentType.Text when Views is not null || Likes is not null:
                throw new DomainException("Metin içeriğine video ölçütü (views/likes) yazılamaz.");
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Views;
        yield return Likes;
        yield return ReadingTime;
        yield return Reactions;
    }
}
