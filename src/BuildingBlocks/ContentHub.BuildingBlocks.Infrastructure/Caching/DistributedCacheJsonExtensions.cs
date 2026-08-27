using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace ContentHub.BuildingBlocks.Infrastructure.Caching;

/// <summary>
/// IDistributedCache üzerinde tip-güvenli JSON yardımcıları. Serileştirme burada,
/// ortak altyapıda yaşar; böylece modül Infrastructure'ında System.Text.Json'a
/// doğrudan bağımlılık oluşmaz (modülün ACL/ArchTest kuralı korunur).
/// </summary>
public static class DistributedCacheJsonExtensions
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static async Task<T?> GetJsonAsync<T>(
        this IDistributedCache cache,
        string key,
        CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(key, cancellationToken).ConfigureAwait(false);
        if (bytes is null || bytes.Length == 0)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(bytes, Options);
    }

    public static Task SetJsonAsync<T>(
        this IDistributedCache cache,
        string key,
        T value,
        TimeSpan absoluteExpirationRelativeToNow,
        CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, Options);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow,
        };
        return cache.SetAsync(key, bytes, options, cancellationToken);
    }
}
