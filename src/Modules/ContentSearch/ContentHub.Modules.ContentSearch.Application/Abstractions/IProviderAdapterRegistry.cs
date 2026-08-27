using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Application.Abstractions;

/// <summary>Biçim → adaptör çözümü. Yeni sağlayıcı = yeni adaptör kaydı (Requirements 8).</summary>
public interface IProviderAdapterRegistry
{
    IProviderAdapter Resolve(ProviderFormat format);
}
