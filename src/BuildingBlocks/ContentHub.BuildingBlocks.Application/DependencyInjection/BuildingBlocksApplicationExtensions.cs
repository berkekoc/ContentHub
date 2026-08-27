using ContentHub.BuildingBlocks.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ContentHub.BuildingBlocks.Application.DependencyInjection;

public static class BuildingBlocksApplicationExtensions
{
    /// <summary>Açık-jenerik MediatR pipeline davranışlarını (doğrulama + günlük) kaydeder.</summary>
    public static IServiceCollection AddBuildingBlocksApplication(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
