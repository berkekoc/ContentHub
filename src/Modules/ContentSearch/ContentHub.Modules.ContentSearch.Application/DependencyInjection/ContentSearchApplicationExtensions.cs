using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ContentHub.Modules.ContentSearch.Application.DependencyInjection;

public static class ContentSearchApplicationExtensions
{
    /// <summary>
    /// Modülün Application servislerini kaydeder: MediatR handler'ları (bu assembly) ve
    /// FluentValidation validator'ları. Pipeline davranışları (doğrulama/günlük) composition
    /// root'ta bir kez kaydedilir (AddBuildingBlocksApplication).
    /// </summary>
    public static IServiceCollection AddContentSearchApplication(this IServiceCollection services)
    {
        var assembly = typeof(ContentSearchApplicationExtensions).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
