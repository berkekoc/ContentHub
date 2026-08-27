using ContentHub.BuildingBlocks.Application.Modules;
using ContentHub.Modules.ContentSearch.Application.DependencyInjection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContentHub.Modules.ContentSearch.Endpoints;

/// <summary>
/// content-search modülünün kayıt + uç eşleme sözleşmesi (fiili ModuleTemplate).
/// Infrastructure kaydı composition root'ta (Api) yapılır; Endpoints Infrastructure'a
/// bağımlı DEĞİLDİR (mimari sınır).
/// </summary>
public sealed class ContentSearchModule : IModule
{
    public string Name => "content-search";

    public IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddContentSearchApplication();
        return services;
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        SearchEndpoints.Map(endpoints);
        IngestEndpoints.Map(endpoints);
    }
}
