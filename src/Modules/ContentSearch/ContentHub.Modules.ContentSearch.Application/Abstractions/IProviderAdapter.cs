using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Application.Abstractions;

/// <summary>
/// Anti-Corruption Layer portu: sağlayıcı biçimini KANONİK FetchedContent akışına
/// çevirir. Sayfalama, dayanıklılık ve biçim ayrıştırma uygulama tarafında (Infrastructure)
/// yaşar; bu port biçimden habersizdir.
/// </summary>
public interface IProviderAdapter
{
    ProviderFormat Format { get; }

    IAsyncEnumerable<FetchedContent> FetchAsync(Provider provider, CancellationToken cancellationToken = default);
}
