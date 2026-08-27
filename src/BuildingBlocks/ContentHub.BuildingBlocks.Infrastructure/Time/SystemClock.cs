using ContentHub.BuildingBlocks.Domain.Abstractions;

namespace ContentHub.BuildingBlocks.Infrastructure.Time;

/// <summary>Gerçek sistem saati. Zamanı dış dünyadan alan tek somut nokta.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
