using ContentHub.BuildingBlocks.Domain.Abstractions;
using ContentHub.BuildingBlocks.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ContentHub.BuildingBlocks.Infrastructure.DependencyInjection;

public static class BuildingBlocksInfrastructureExtensions
{
    public static IServiceCollection AddBuildingBlocksInfrastructure(this IServiceCollection services)
    {
        services.TryAddSingleton<IClock, SystemClock>();
        return services;
    }
}
