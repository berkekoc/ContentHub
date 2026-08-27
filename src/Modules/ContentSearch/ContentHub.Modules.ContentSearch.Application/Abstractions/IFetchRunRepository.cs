using ContentHub.BuildingBlocks.Application.Models;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Application.Abstractions;

public interface IFetchRunRepository
{
    Task AddAsync(ProviderFetchRun run, CancellationToken cancellationToken = default);

    void Update(ProviderFetchRun run);

    Task<PagedResult<FetchRunDto>> ListAsync(
        Guid? providerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
