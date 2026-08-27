namespace ContentHub.Modules.ContentSearch.Infrastructure.Caching;

public sealed class SearchCacheOptions
{
    public const string SectionName = "ContentSearch:Cache";

    public string KeyPrefix { get; set; } = "search";

    /// <summary>Sayfa girdisi ömrü. Sürüm-jetonu geçersizleştirmesi bundan bağımsız çalışır.</summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(5);
}
