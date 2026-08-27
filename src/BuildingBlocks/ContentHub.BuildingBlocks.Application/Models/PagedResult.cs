namespace ContentHub.BuildingBlocks.Application.Models;

/// <summary>Offset tabanlı sayfalama sonucu; toplam sayı bilinir (Norms 7).</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount)
{
    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Empty(int page, int pageSize)
        => new(Array.Empty<T>(), page, pageSize, 0);
}
