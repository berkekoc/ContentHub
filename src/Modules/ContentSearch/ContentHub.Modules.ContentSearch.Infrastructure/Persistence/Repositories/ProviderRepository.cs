using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Domain.Model;
using ContentHub.Modules.ContentSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Persistence.Repositories;

internal sealed class ProviderRepository : IProviderRepository
{
    private readonly ContentSearchDbContext _db;

    public ProviderRepository(ContentSearchDbContext db) => _db = db;

    public Task<Provider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.Providers.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Provider>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await _db.Providers
            .Where(p => p.Status == ProviderStatus.Active)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Provider>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Providers
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task AddAsync(Provider provider, CancellationToken cancellationToken = default)
        => await _db.Providers.AddAsync(provider, cancellationToken).ConfigureAwait(false);
}
