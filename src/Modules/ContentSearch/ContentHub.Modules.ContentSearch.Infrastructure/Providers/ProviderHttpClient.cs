using System.Net;
using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Domain.Model;
using Polly.CircuitBreaker;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Providers;

/// <summary>
/// Sağlayıcıya sayfa çeken tiplendirilmiş istemci. Dayanıklılık (retry + circuit breaker)
/// DI'da AddStandardResilienceHandler ile bağlanır; burada giden istek limiti uygulanır ve
/// kalıcı 429/devre-kesik durumları port istisnalarına çevrilir (Application sözleşmesi).
/// </summary>
internal sealed class ProviderHttpClient
{
    private readonly HttpClient _http;
    private readonly IOutboundRateLimiter _rateLimiter;

    public ProviderHttpClient(HttpClient http, IOutboundRateLimiter rateLimiter)
    {
        _http = http;
        _rateLimiter = rateLimiter;
    }

    public async Task<string> GetPageAsync(
        Provider provider,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await _rateLimiter.WaitForSlotAsync(provider.Id, provider.RateLimitPolicy, cancellationToken)
            .ConfigureAwait(false);

        var url = $"{provider.BaseUrl.TrimEnd('/')}?page={page}&pageSize={pageSize}";

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        }
        catch (BrokenCircuitException ex)
        {
            throw new ProviderUnavailableException($"Sağlayıcı devre kesildi: {provider.Name}", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ProviderUnavailableException($"Sağlayıcıya ulaşılamadı: {provider.Name}", ex);
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            throw new ProviderRateLimitedException($"Sağlayıcı istek limiti aşıldı: {provider.Name}");
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }
}
