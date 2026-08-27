using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContentHub.BuildingBlocks.Application.Modules;

/// <summary>
/// Modüler monolit kayıt sözleşmesi. Her modül kendi servislerini kaydeder ve
/// uçlarını eşler; composition root (Api) modülleri keşfedip bu sözleşmeyi çağırır.
/// </summary>
public interface IModule
{
    string Name { get; }

    IServiceCollection RegisterServices(IServiceCollection services, IConfiguration configuration);

    void MapEndpoints(IEndpointRouteBuilder endpoints);
}
