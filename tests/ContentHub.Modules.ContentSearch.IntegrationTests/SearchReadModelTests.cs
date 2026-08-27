using ContentHub.BuildingBlocks.Application.Models;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Domain.Model;
using ContentHub.Modules.ContentSearch.Infrastructure.Persistence;
using ContentHub.Modules.ContentSearch.Infrastructure.ReadModel;
using ContentHub.Modules.ContentSearch.IntegrationTests.Fixtures;
using Microsoft.Extensions.Options;
using Xunit;

namespace ContentHub.Modules.ContentSearch.IntegrationTests;

/// <summary>Okuma modeli (O7): tekilleştirme, temsilci seçimi, sıralama, boş sonuç, güncellik.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class SearchReadModelTests
{
    private readonly PostgresFixture _fixture;
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

    public SearchReadModelTests(PostgresFixture fixture) => _fixture = fixture;

    private SearchReadModel CreateReadModel(ContentSearchDbContext context)
        => new(context, Options.Create(new SearchReadOptions()));

    private static ContentItem Video(Guid providerId, string ext, string title, DateTimeOffset publishedAt, long views, long likes)
        => ContentItem.Create(providerId, ExternalId.Create(ext), title, null, ContentType.Video, publishedAt, null,
            ContentMetrics.ForVideo(views, likes), Now);

    [Fact]
    public async Task Search_DuplicateAcrossProviders_CollapsesToHighestScoringRepresentative()
    {
        var providerA = Provider.Create("A", ProviderFormat.Json, "https://a");
        var providerB = Provider.Create("B", ProviderFormat.Xml, "https://b");
        var published = Now.AddDays(-1);

        // Aynı başlık+tür+tarih → aynı parmak izi (iki sağlayıcıda). B daha yüksek skorlu.
        var lowRep = Video(providerA.Id, "a1", "zqx tekil konu basligi", published, 1000, 100);
        var highRep = Video(providerB.Id, "b1", "zqx tekil konu basligi", published, 9000, 900);
        var solo = Video(providerA.Id, "a2", "zqx yalniz konu", published, 500, 50);

        await using (var seed = _fixture.CreateContext())
        {
            seed.Providers.AddRange(providerA, providerB);
            seed.ContentItems.AddRange(lowRep, highRep, solo);
            await seed.SaveChangesAsync();
        }

        await using var context = _fixture.CreateContext();
        var result = await CreateReadModel(context).SearchAsync(
            new SearchCriteria("zqx", null, SortOption.Popularity, 1, 20), Now);

        Assert.Equal(2, result.TotalCount); // dup grubu + solo
        var representative = result.Items.Single(i => i.ProviderCount == 2);
        Assert.Equal(highRep.Id, representative.Id); // temsilci = en yüksek final_score
        Assert.Contains(result.Items, i => i.Id == solo.Id && i.ProviderCount == 1);
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmptyPage_NotError()
    {
        await using var context = _fixture.CreateContext();
        var result = await CreateReadModel(context).SearchAsync(
            new SearchCriteria("kesinlikleboylebirkelimeyok", null, SortOption.Relevance, 1, 20), Now);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Search_SameQueryTwice_ReturnsSameStableOrder()
    {
        var provider = Provider.Create("Stable", ProviderFormat.Json, "https://s");
        await using (var seed = _fixture.CreateContext())
        {
            seed.Providers.Add(provider);
            for (var i = 0; i < 5; i++)
            {
                seed.ContentItems.Add(Video(provider.Id, $"stable-{i}", $"kararli konu {i}", Now.AddDays(-i), 1000, 100));
            }

            await seed.SaveChangesAsync();
        }

        await using var context = _fixture.CreateContext();
        var readModel = CreateReadModel(context);
        var criteria = new SearchCriteria("kararli", null, SortOption.Popularity, 1, 20);

        var first = await readModel.SearchAsync(criteria, Now);
        var second = await readModel.SearchAsync(criteria, Now);

        Assert.Equal(
            first.Items.Select(i => i.Id),
            second.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task Search_RecencyReducesFinalScore_AsNowAdvances()
    {
        var provider = Provider.Create("Recency", ProviderFormat.Json, "https://r");
        var item = Video(provider.Id, "rec-1", "guncellik konu ornegi", Now.AddDays(-2), 1000, 100);
        await using (var seed = _fixture.CreateContext())
        {
            seed.Providers.Add(provider);
            seed.ContentItems.Add(item);
            await seed.SaveChangesAsync();
        }

        await using var context = _fixture.CreateContext();
        var readModel = CreateReadModel(context);
        var criteria = new SearchCriteria("guncellik", null, SortOption.Popularity, 1, 20);

        var fresh = await readModel.SearchAsync(criteria, Now);                 // yayın now-2g → +5
        var later = await readModel.SearchAsync(criteria, Now.AddMonths(2));    // artık 2 aydan eski → +1

        var freshScore = fresh.Items.Single(i => i.Id == item.Id).FinalScore;
        var laterScore = later.Items.Single(i => i.Id == item.Id).FinalScore;
        Assert.True(laterScore < freshScore, $"Beklenen düşüş yok: {laterScore} >= {freshScore}");
    }
}
