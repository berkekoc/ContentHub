using System.Reflection;
using ContentHub.BuildingBlocks.Application.Modules;

namespace ContentHub.Api.Modules;

/// <summary>Verilen assembly'lerdeki IModule uygulamalarını keşfeder (modüler monolit kaydı).</summary>
public static class ModuleRegistrar
{
    public static IReadOnlyList<IModule> DiscoverModules(params Assembly[] assemblies)
        => assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IModule).IsAssignableFrom(type)
                           && type is { IsAbstract: false, IsInterface: false })
            .Select(type => (IModule)Activator.CreateInstance(type)!)
            .OrderBy(module => module.Name, StringComparer.Ordinal)
            .ToList();
}
