using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Providers;

/// <summary>Biçim → adaptör çözümü. Yeni biçim = yeni IProviderAdapter kaydı (Requirements 8).</summary>
internal sealed class ProviderAdapterRegistry : IProviderAdapterRegistry
{
    private readonly IReadOnlyDictionary<ProviderFormat, IProviderAdapter> _adapters;

    public ProviderAdapterRegistry(IEnumerable<IProviderAdapter> adapters)
        => _adapters = adapters.ToDictionary(a => a.Format);

    public IProviderAdapter Resolve(ProviderFormat format)
        => _adapters.TryGetValue(format, out var adapter)
            ? adapter
            : throw new NotSupportedException($"Bu biçim için adaptör kayıtlı değil: {format}");
}
