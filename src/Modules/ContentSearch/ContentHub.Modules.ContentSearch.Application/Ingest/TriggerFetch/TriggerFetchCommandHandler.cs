using ContentHub.BuildingBlocks.Domain.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Domain.Model;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ContentHub.Modules.ContentSearch.Application.Ingest.TriggerFetch;

internal sealed class TriggerFetchCommandHandler
    : IRequestHandler<TriggerFetchCommand, FetchSummaryDto>
{
    private readonly IProviderRepository _providerRepository;
    private readonly IContentRepository _contentRepository;
    private readonly IFetchRunRepository _fetchRunRepository;
    private readonly IProviderAdapterRegistry _adapterRegistry;
    private readonly ISearchResultCache _searchResultCache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<TriggerFetchCommandHandler> _logger;

    public TriggerFetchCommandHandler(
        IProviderRepository providerRepository,
        IContentRepository contentRepository,
        IFetchRunRepository fetchRunRepository,
        IProviderAdapterRegistry adapterRegistry,
        ISearchResultCache searchResultCache,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<TriggerFetchCommandHandler> logger)
    {
        _providerRepository = providerRepository;
        _contentRepository = contentRepository;
        _fetchRunRepository = fetchRunRepository;
        _adapterRegistry = adapterRegistry;
        _searchResultCache = searchResultCache;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<FetchSummaryDto> Handle(TriggerFetchCommand request, CancellationToken cancellationToken)
    {
        var providers = await ResolveTargetsAsync(request.ProviderId, cancellationToken).ConfigureAwait(false);
        var results = new List<ProviderFetchResultDto>(providers.Count);
        var anySucceeded = false;

        foreach (var provider in providers)
        {
            var result = await FetchOneAsync(provider, cancellationToken).ConfigureAwait(false);
            results.Add(result);
            if (result.Status == nameof(FetchRunStatus.Succeeded))
            {
                anySucceeded = true;
            }
        }

        // O10: başarılı çekim arama önbelleğini geçersiz kılar (bayat sonuç gösterilmez).
        if (anySucceeded)
        {
            await _searchResultCache.InvalidateAsync(cancellationToken).ConfigureAwait(false);
        }

        return new FetchSummaryDto(results);
    }

    private async Task<IReadOnlyList<Provider>> ResolveTargetsAsync(Guid? providerId, CancellationToken cancellationToken)
    {
        if (providerId is null)
        {
            return await _providerRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        }

        var provider = await _providerRepository.GetByIdAsync(providerId.Value, cancellationToken).ConfigureAwait(false);
        return provider is null ? Array.Empty<Provider>() : new[] { provider };
    }

    private async Task<ProviderFetchResultDto> FetchOneAsync(Provider provider, CancellationToken cancellationToken)
    {
        var run = ProviderFetchRun.Start(provider.Id, _clock.UtcNow);
        await _fetchRunRepository.AddAsync(run, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var incoming = 0;
        var created = 0;
        var updated = 0;
        var processed = new Dictionary<string, ContentItem>(StringComparer.Ordinal);

        try
        {
            var adapter = _adapterRegistry.Resolve(provider.Format);

            await foreach (var fetched in adapter.FetchAsync(provider, cancellationToken).ConfigureAwait(false))
            {
                incoming++;

                // Aynı çekim içinde tekrar eden doğal anahtar → yerel güncelleme (kopya insert yok).
                if (processed.TryGetValue(fetched.ExternalId, out var pending))
                {
                    pending.UpdateFrom(fetched.Title, fetched.Description, fetched.PublishedAt, fetched.SourceUrl, fetched.ToMetrics(), _clock.UtcNow);
                    continue;
                }

                var externalId = ExternalId.Create(fetched.ExternalId);
                var existing = await _contentRepository
                    .GetByNaturalKeyAsync(provider.Id, externalId, cancellationToken)
                    .ConfigureAwait(false);

                if (existing is null)
                {
                    var item = ContentItem.Create(
                        provider.Id,
                        externalId,
                        fetched.Title,
                        fetched.Description,
                        fetched.Type,
                        fetched.PublishedAt,
                        fetched.SourceUrl,
                        fetched.ToMetrics(),
                        _clock.UtcNow);

                    await _contentRepository.AddAsync(item, cancellationToken).ConfigureAwait(false);
                    processed[fetched.ExternalId] = item;
                    created++;
                }
                else
                {
                    existing.UpdateFrom(fetched.Title, fetched.Description, fetched.PublishedAt, fetched.SourceUrl, fetched.ToMetrics(), _clock.UtcNow);
                    _contentRepository.Update(existing);
                    processed[fetched.ExternalId] = existing;
                    updated++;
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            run.Succeed(_clock.UtcNow, incoming, created, updated);
            _fetchRunRepository.Update(run);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Çekim tamamlandı: sağlayıcı={Provider} gelen={Incoming} yeni={New} güncellenen={Updated}",
                provider.Name, incoming, created, updated);

            return new ProviderFetchResultDto(provider.Id, provider.Name, nameof(FetchRunStatus.Succeeded), incoming, created, updated, null);
        }
        catch (ProviderRateLimitedException ex)
        {
            run.MarkRateLimited(_clock.UtcNow, ex.Message);
            _fetchRunRepository.Update(run);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogWarning(ex, "Çekim istek limitine takıldı: sağlayıcı={Provider}", provider.Name);
            return new ProviderFetchResultDto(provider.Id, provider.Name, nameof(FetchRunStatus.RateLimited), incoming, created, updated, ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            run.Fail(_clock.UtcNow, ex.Message);
            _fetchRunRepository.Update(run);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError(ex, "Çekim hata verdi: sağlayıcı={Provider}", provider.Name);
            return new ProviderFetchResultDto(provider.Id, provider.Name, nameof(FetchRunStatus.Failed), incoming, created, updated, ex.Message);
        }
    }
}
