using System.Security.Cryptography;
using System.Text;
using ContentHub.BuildingBlocks.Application.Models;
using ContentHub.BuildingBlocks.Infrastructure.Caching;
using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Caching;

/// <summary>
/// Arama sonucu sayfa önbelleği (O10). Anahtar = "{prefix}:v{token}:{hash(kriter)}".
/// Sürüm-jetonu (token) global sayaçtır; başarılı çekimden sonra InvalidateAsync jetonu
/// değiştirir → eski sayfalar erişilemez olur (bayat sonuç gösterilmez). Tag desteksiz
/// IDistributedCache için doğru O(1) geçersizleştirme deseni.
/// </summary>
internal sealed class DistributedSearchResultCache : ISearchResultCache
{
    private readonly IDistributedCache _cache;
    private readonly SearchCacheOptions _options;

    public DistributedSearchResultCache(IDistributedCache cache, IOptions<SearchCacheOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    private string TokenKey => $"{_options.KeyPrefix}:token";

    public async Task<PagedResult<ContentItemDto>?> GetAsync(SearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync(cancellationToken).ConfigureAwait(false);
        var key = BuildKey(token, criteria);
        return await _cache.GetJsonAsync<PagedResult<ContentItemDto>>(key, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetAsync(SearchCriteria criteria, PagedResult<ContentItemDto> result, CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync(cancellationToken).ConfigureAwait(false);
        var key = BuildKey(token, criteria);
        await _cache.SetJsonAsync(key, result, _options.Ttl, cancellationToken).ConfigureAwait(false);
    }

    public Task InvalidateAsync(CancellationToken cancellationToken = default)
        => _cache.SetStringAsync(TokenKey, NewToken(), cancellationToken);

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        var token = await _cache.GetStringAsync(TokenKey, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(token))
        {
            return token;
        }

        token = NewToken();
        await _cache.SetStringAsync(TokenKey, token, cancellationToken).ConfigureAwait(false);
        return token;
    }

    private string BuildKey(string token, SearchCriteria criteria)
    {
        var canonical = string.Join(
            '|',
            criteria.Keyword ?? string.Empty,
            criteria.ContentType?.ToString() ?? "all",
            criteria.Sort.ToString(),
            criteria.Page.ToString(),
            criteria.PageSize.ToString());

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        return $"{_options.KeyPrefix}:v{token}:{hash}";
    }

    private static string NewToken() => Guid.CreateVersion7().ToString("n");
}
