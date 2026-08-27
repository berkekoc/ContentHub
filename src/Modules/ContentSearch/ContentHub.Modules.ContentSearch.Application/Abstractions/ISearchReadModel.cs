using ContentHub.BuildingBlocks.Application.Models;
using ContentHub.Modules.ContentSearch.Application.Contracts;

namespace ContentHub.Modules.ContentSearch.Application.Abstractions;

/// <summary>
/// Arama okuma projeksiyonu (CQRS okuma yolu). Ham SQL: FTS eşleşme + alakalılık +
/// güncellik (okuma anı) + tekilleştirme + sıralama + offset sayfalama. No-tracking.
/// Güncellik hesabı için 'now' dışarıdan verilir (S1, test edilebilirlik).
/// </summary>
public interface ISearchReadModel
{
    Task<PagedResult<ContentItemDto>> SearchAsync(
        SearchCriteria criteria,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
