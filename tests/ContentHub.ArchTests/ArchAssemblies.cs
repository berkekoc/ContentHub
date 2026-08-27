using System.Reflection;

namespace ContentHub.ArchTests;

/// <summary>ArchTest'lerin denetlediği modül assembly'lerine tek noktadan erişim.</summary>
internal static class ArchAssemblies
{
    public static readonly Assembly Domain =
        typeof(Modules.ContentSearch.Domain.Model.ContentItem).Assembly;

    public static readonly Assembly Application =
        typeof(Modules.ContentSearch.Application.Search.SearchContent.SearchContentQuery).Assembly;

    public static readonly Assembly Infrastructure =
        typeof(Modules.ContentSearch.Infrastructure.DependencyInjection).Assembly;

    public static readonly Assembly Endpoints =
        typeof(Modules.ContentSearch.Endpoints.ContentSearchModule).Assembly;

    public static readonly Assembly Api = typeof(Program).Assembly;
}
