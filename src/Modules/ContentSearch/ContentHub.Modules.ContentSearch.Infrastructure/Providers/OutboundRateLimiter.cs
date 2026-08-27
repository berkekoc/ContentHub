using System.Collections.Concurrent;
using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Providers;

/// <summary>
/// Sağlayıcı başına GİDEN çağrıları politikayla (varsayılan 60/dk, S6) oranlar. Minimum
/// istekler-arası aralık = 60/rpm sn; sağlayıcı limiti korunur, arama tarafı etkilenmez.
/// </summary>
internal sealed class OutboundRateLimiter : IOutboundRateLimiter
{
    private readonly ConcurrentDictionary<Guid, ProviderGate> _gates = new();

    public async Task WaitForSlotAsync(
        Guid providerId,
        RateLimitPolicy policy,
        CancellationToken cancellationToken = default)
    {
        var gate = _gates.GetOrAdd(providerId, static _ => new ProviderGate());
        var minInterval = TimeSpan.FromSeconds(60.0 / policy.RequestsPerMinute);

        await gate.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var earliest = gate.LastRequestUtc + minInterval;
            if (earliest > now)
            {
                await Task.Delay(earliest - now, cancellationToken).ConfigureAwait(false);
            }

            gate.LastRequestUtc = DateTimeOffset.UtcNow;
        }
        finally
        {
            gate.Semaphore.Release();
        }
    }

    private sealed class ProviderGate
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public DateTimeOffset LastRequestUtc { get; set; } = DateTimeOffset.MinValue;
    }
}
