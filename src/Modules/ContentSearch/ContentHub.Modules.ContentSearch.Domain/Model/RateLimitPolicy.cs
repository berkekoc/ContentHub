using ContentHub.BuildingBlocks.Domain;

namespace ContentHub.Modules.ContentSearch.Domain.Model;

/// <summary>Sağlayıcı başına birim zamandaki izinli istek sayısı ve aşım davranışı (Norms 11, S6).</summary>
public sealed class RateLimitPolicy : ValueObject
{
    public const int DefaultRequestsPerMinute = 60;

    private RateLimitPolicy(int requestsPerMinute, OverflowBehavior overflowBehavior)
    {
        RequestsPerMinute = requestsPerMinute;
        OverflowBehavior = overflowBehavior;
    }

    public int RequestsPerMinute { get; }

    public OverflowBehavior OverflowBehavior { get; }

    public static RateLimitPolicy Default => new(DefaultRequestsPerMinute, OverflowBehavior.Wait);

    public static RateLimitPolicy Create(int requestsPerMinute, OverflowBehavior overflowBehavior)
    {
        if (requestsPerMinute <= 0)
        {
            throw new DomainException("İstek limiti pozitif olmalıdır.");
        }

        return new RateLimitPolicy(requestsPerMinute, overflowBehavior);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RequestsPerMinute;
        yield return OverflowBehavior;
    }
}
