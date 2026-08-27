using NetArchTest.Rules;
using Xunit;

namespace ContentHub.ArchTests;

/// <summary>Katman bağımlılık yönü: dışa doğru yasak, içe doğru serbest (CLAUDE.md kural 3).</summary>
public sealed class LayerDependencyTests
{
    private const string DomainNs = "ContentHub.Modules.ContentSearch.Domain";
    private const string ApplicationNs = "ContentHub.Modules.ContentSearch.Application";
    private const string InfrastructureNs = "ContentHub.Modules.ContentSearch.Infrastructure";
    private const string DbContextNs = "ContentHub.Modules.ContentSearch.Infrastructure.Persistence";

    [Fact]
    public void Domain_ShouldNotDependOn_OuterLayersOrFrameworks()
    {
        var result = Types.InAssembly(ArchAssemblies.Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNs,
                InfrastructureNs,
                "MediatR",
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "System.Net.Http",
                "System.Text.Json",
                "System.Xml")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Domain ihlali", result));
    }

    [Fact]
    public void Application_ShouldNotDependOn_InfrastructureOrPersistenceTech()
    {
        var result = Types.InAssembly(ArchAssemblies.Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                InfrastructureNs,
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Application ihlali", result));
    }

    [Fact]
    public void Endpoints_ShouldNotDependOn_DbContext()
    {
        var result = Types.InAssembly(ArchAssemblies.Endpoints)
            .ShouldNot()
            .HaveDependencyOn(DbContextNs)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Endpoints→DbContext ihlali", result));
    }

    [Fact]
    public void Api_ShouldNotDependOn_DbContextDirectly()
    {
        var result = Types.InAssembly(ArchAssemblies.Api)
            .ShouldNot()
            .HaveDependencyOn(DbContextNs)
            .GetResult();

        Assert.True(result.IsSuccessful, Describe("Api→DbContext ihlali", result));
    }

    internal static string Describe(string title, TestResult result)
    {
        var names = result.FailingTypeNames ?? Array.Empty<string>();
        return $"{title}: {string.Join(", ", names)}";
    }
}
