using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Application.Abstractions;

/// <summary>ContentItem aggregate yazma deposu. Doğal anahtarla idempotent upsert'i besler.</summary>
public interface IContentRepository
{
    Task<ContentItem?> GetByNaturalKeyAsync(Guid providerId, ExternalId externalId, CancellationToken cancellationToken = default);

    Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(ContentItem item, CancellationToken cancellationToken = default);

    void Update(ContentItem item);
}
