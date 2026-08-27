using NetArchTest.Rules;
using Xunit;

namespace ContentHub.ArchTests;

public sealed class PurityAndAclTests
{
    private const string ProvidersNs = "ContentHub.Modules.ContentSearch.Infrastructure.Providers";

    [Fact]
    public void ScoringService_ShouldNotDependOn_ClockOrIo()
    {
        // CLAUDE.md kural 2 / ArchTest kuralı 5: puanlama saf; zaman parametreyle gelir.
        var result = Types.InAssembly(ArchAssemblies.Domain)
            .That()
            .HaveName("ScoringService")
            .ShouldNot()
            .HaveDependencyOnAny(
                "ContentHub.BuildingBlocks.Domain.Abstractions",
                "System.Net.Http",
                "Microsoft.EntityFrameworkCore",
                "Npgsql")
            .GetResult();

        Assert.True(result.IsSuccessful, LayerDependencyTests.Describe("ScoringService saflık ihlali", result));
    }

    [Fact]
    public void ProviderFormatTypes_ShouldOnlyLive_UnderProvidersNamespace()
    {
        // ACL (CLAUDE.md kural 1 / ArchTest kuralı 4): System.Text.Json / System.Xml yalnız Providers'ta.
        var result = Types.InAssembly(ArchAssemblies.Infrastructure)
            .That()
            .DoNotResideInNamespace(ProvidersNs)
            .ShouldNot()
            .HaveDependencyOnAny("System.Text.Json", "System.Xml")
            .GetResult();

        Assert.True(result.IsSuccessful, LayerDependencyTests.Describe("ACL biçim sızması", result));
    }

    [Fact]
    public void Handlers_ShouldBeSealed()
    {
        var result = Types.InAssembly(ArchAssemblies.Application)
            .That()
            .HaveNameEndingWith("Handler")
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .GetResult();

        Assert.True(result.IsSuccessful, LayerDependencyTests.Describe("Handler sealed değil", result));
    }
}
