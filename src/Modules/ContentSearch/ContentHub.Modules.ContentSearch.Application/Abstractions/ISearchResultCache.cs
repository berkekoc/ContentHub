using ContentHub.BuildingBlocks.Application.Models;
using ContentHub.Modules.ContentSearch.Application.Contracts;

namespace ContentHub.Modules.ContentSearch.Application.Abstractions;

/// <summary>
/// Arama sonucu sayfa önbelleği. Sürüm-jetonu ile O(1) geçersizleştirme: başarılı bir
/// çekim jetonu artırır, eski sayfalar erişilemez olur (bayat sonuç gösterilmez, O10).
/// </summary>
public interface ISearchResultCache
{
    Task<PagedResult<ContentItemDto>?> GetAsync(SearchCriteria criteria, CancellationToken cancellationToken = default);

    Task SetAsync(SearchCriteria criteria, PagedResult<ContentItemDto> result, CancellationToken cancellationToken = default);

    Task InvalidateAsync(CancellationToken cancellationToken = default);
}
