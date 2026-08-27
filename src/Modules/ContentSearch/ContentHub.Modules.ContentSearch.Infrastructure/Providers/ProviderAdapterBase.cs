using System.Runtime.CompilerServices;
using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Domain.Model;
using Microsoft.Extensions.Logging;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Providers;

/// <summary>
/// Adaptörlerin ortak sayfalama/çekme akışı (ACL). Biçime özel ayrıştırma alt sınıfa
/// bırakılır. Tek bir bozuk KAYIT çekimi düşürmez — ayrıştırma alt sınıfta kayıt bazında
/// korunur; yalnızca tüm sayfa okunamazsa akış güvenli biçimde sonlanır.
/// </summary>
internal abstract class ProviderAdapterBase : IProviderAdapter
{
    protected const int PageSize = 100;
    private const int MaxPages = 10_000;

    private readonly ProviderHttpClient _client;
    private readonly ILogger _logger;

    protected ProviderAdapterBase(ProviderHttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    public abstract ProviderFormat Format { get; }

    protected abstract IReadOnlyList<FetchedContent> ParsePage(string payload, Provider provider);

    public async IAsyncEnumerable<FetchedContent> FetchAsync(
        Provider provider,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var page = 1;

        while (page <= MaxPages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var payload = await _client.GetPageAsync(provider, page, PageSize, cancellationToken)
                .ConfigureAwait(false);

            IReadOnlyList<FetchedContent> items;
            try
            {
                items = ParsePage(payload, provider);
            }
            catch (Exception ex)
            {
                // Tüm sayfa ayrıştırılamadı (biçim bozulması) — akışı güvenli sonlandır, çekim düşmesin.
                _logger.LogError(ex, "Sayfa ayrıştırılamadı: sağlayıcı={Provider} sayfa={Page}", provider.Name, page);
                yield break;
            }

            foreach (var item in items)
            {
                yield return item;
            }

            if (items.Count < PageSize)
            {
                yield break;
            }

            page++;
        }
    }
}
