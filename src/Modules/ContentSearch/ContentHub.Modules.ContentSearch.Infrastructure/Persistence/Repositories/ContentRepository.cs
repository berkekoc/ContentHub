using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Domain.Model;
using ContentHub.Modules.ContentSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Persistence.Repositories;

internal sealed class ContentRepository : IContentRepository
{
    private readonly ContentSearchDbContext _db;

    public ContentRepository(ContentSearchDbContext db) => _db = db;

    public async Task<ContentItem?> GetByNaturalKeyAsync(
        Guid providerId,
        ExternalId externalId,
        CancellationToken cancellationToken = default)
    {
        // Doğal anahtar (idempotency, İHLAL-EDİLEMEZ): ProviderId SQL'de filtrelenir; ExternalId
        // bir value object olup özel '==' operatörü taşıdığından SQL'e çevrilemez — bu yüzden
        // BELLEKTE eşleştirilir. Sağlayıcı başına kayıt sayısı küçük (case ölçeği), güvenli/determinist.
        var candidates = await _db.ContentItems
            .Where(c => c.ProviderId == providerId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return candidates.FirstOrDefault(c => c.ExternalId == externalId);
    }

    public Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.ContentItems.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(ContentItem item, CancellationToken cancellationToken = default)
        => await _db.ContentItems.AddAsync(item, cancellationToken).ConfigureAwait(false);

    public void Update(ContentItem item) => _db.ContentItems.Update(item);
}
