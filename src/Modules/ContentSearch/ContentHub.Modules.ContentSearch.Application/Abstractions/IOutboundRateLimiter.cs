using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Application.Abstractions;

/// <summary>Sağlayıcıya giden çağrıları politika (varsayılan 60/dk, S6) ile oranlar.</summary>
public interface IOutboundRateLimiter
{
    Task WaitForSlotAsync(Guid providerId, RateLimitPolicy policy, CancellationToken cancellationToken = default);
}
