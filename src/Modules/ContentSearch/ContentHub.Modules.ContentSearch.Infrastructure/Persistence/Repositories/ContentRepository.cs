using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Domain.Model;
using ContentHub.Modules.ContentSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Persistence.Repositories;

internal sealed class ContentRepository : IContentRepository
{
    private readonly ContentSearchDbContext _db;

    public ContentRepository(ContentSearchDbContext db) => _db = db;

    public Task<ContentItem?> GetByNaturalKeyAsync(
        Guid providerId,
        ExternalId externalId,
        CancellationToken cancellationToken = default)
        => _db.ContentItems
            .FirstOrDefaultAsync(
                c => c.ProviderId == providerId && c.ExternalId == externalId,
                cancellationToken);

    public Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.ContentItems.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(ContentItem item, CancellationToken cancellationToken = default)
        => await _db.ContentItems.AddAsync(item, cancellationToken).ConfigureAwait(false);

    public void Update(ContentItem item) => _db.ContentItems.Update(item);
}
