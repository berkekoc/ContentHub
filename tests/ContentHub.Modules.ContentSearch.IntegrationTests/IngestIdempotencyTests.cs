using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Application.Ingest.TriggerFetch;
using ContentHub.Modules.ContentSearch.Domain.Model;
using ContentHub.Modules.ContentSearch.Infrastructure;
using ContentHub.Modules.ContentSearch.Infrastructure.Persistence;
using ContentHub.Modules.ContentSearch.IntegrationTests.Fixtures;
using ContentHub.BuildingBlocks.Application.DependencyInjection;
using ContentHub.BuildingBlocks.Infrastructure.DependencyInjection;
using ContentHub.Modules.ContentSearch.Application.DependencyInjection;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace ContentHub.Modules.ContentSearch.IntegrationTests;

/// <summary>
/// İHLAL-EDİLEMEZ (O6 DoD / idempotency): WireMock JSON sağlayıcısı; iki kez çekim →
/// içerik sayısı SABİT (kopya yok), ikinci çalıştırmada updated doğru. Bozuk kayıt (views=0)
/// çekimi düşürmez, yine listelenir.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class IngestIdempotencyTests
{
    private readonly PostgresFixture _fixture;

    public IngestIdempotencyTests(PostgresFixture fixture) => _fixture = fixture;

    private const string JsonBody = """
    {
      "page": 1, "pageSize": 100, "total": 3,
      "items": [
        { "id": "i1", "title": "wiremock konu bir", "description": "aciklama", "type": "video", "publishedAt": "2026-01-10T00:00:00Z", "url": "https://x/1", "metrics": { "views": 1000, "likes": 50 } },
        { "id": "i2", "title": "wiremock konu iki", "type": "text", "publishedAt": "2026-01-05T00:00:00Z", "metrics": { "readingTime": 5, "reactions": 20 } },
        { "id": "i3", "title": "wiremock bozuk kayit", "type": "video", "publishedAt": "2026-01-01T00:00:00Z", "metrics": { "views": 0, "likes": 5 } }
      ]
    }
    """;

    private ServiceProvider BuildServices()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ContentHub"] = _fixture.ConnectionString,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddLogging();
        services.AddDistributedMemoryCache();
        services.AddBuildingBlocksApplication();
        services.AddBuildingBlocksInfrastructure();
        services.AddContentSearchApplication();
        services.AddContentSearchInfrastructure(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task TriggerFetch_Twice_IsIdempotent_AndKeepsBrokenRecord()
    {
        using var mock = WireMockServer.Start();
        mock.Given(Request.Create().WithPath("/api/json").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonBody));

        await using var services = BuildServices();

        var provider = Provider.Create("WireMock JSON", ProviderFormat.Json, $"{mock.Url}/api/json");
        await using (var scope = services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ContentSearchDbContext>();
            db.Providers.Add(provider);
            await db.SaveChangesAsync();
        }

        FetchSummaryDto first;
        await using (var scope = services.CreateAsyncScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            first = await sender.Send(new TriggerFetchCommand(provider.Id));
        }

        Assert.Equal(nameof(FetchRunStatus.Succeeded), first.Providers[0].Status);
        Assert.Equal(3, first.Providers[0].Incoming);
        Assert.Equal(3, first.Providers[0].New);
        Assert.Equal(0, first.Providers[0].Updated);

        FetchSummaryDto second;
        await using (var scope = services.CreateAsyncScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            second = await sender.Send(new TriggerFetchCommand(provider.Id));
        }

        Assert.Equal(0, second.Providers[0].New);      // kopya yaratılmaz
        Assert.Equal(3, second.Providers[0].Updated);  // hepsi güncellenir

        await using (var verify = _fixture.CreateContext())
        {
            var items = await verify.ContentItems.Where(c => c.ProviderId == provider.Id).ToListAsync();
            Assert.Equal(3, items.Count); // idempotency: sayı sabit

            // Bozuk kayıt (views=0) yine mevcut, etkileşim 0 ile.
            var broken = items.Single(c => c.ExternalId.Value == "i3");
            Assert.Equal(0m, broken.Score.EngagementScore);
        }
    }
}
