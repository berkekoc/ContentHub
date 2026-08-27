using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Application.Abstractions;

public interface IProviderRepository
{
    Task<Provider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Provider>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Provider provider, CancellationToken cancellationToken = default);
}
