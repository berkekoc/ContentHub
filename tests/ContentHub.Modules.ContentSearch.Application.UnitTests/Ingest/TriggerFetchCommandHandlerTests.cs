using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Application.Ingest.TriggerFetch;
using ContentHub.Modules.ContentSearch.Application.UnitTests.TestDoubles;
using ContentHub.Modules.ContentSearch.Domain.Model;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace ContentHub.Modules.ContentSearch.Application.UnitTests.Ingest;

public sealed class TriggerFetchCommandHandlerTests
{
    private readonly IProviderRepository _providers = Substitute.For<IProviderRepository>();
    private readonly IContentRepository _content = Substitute.For<IContentRepository>();
    private readonly IFetchRunRepository _fetchRuns = Substitute.For<IFetchRunRepository>();
    private readonly IProviderAdapterRegistry _registry = Substitute.For<IProviderAdapterRegistry>();
    private readonly ISearchResultCache _cache = Substitute.For<ISearchResultCache>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));

    private readonly Provider _provider = Provider.Create("Json Provider", ProviderFormat.Json, "https://p/json");

    private TriggerFetchCommandHandler CreateHandler() => new(
        _providers, _content, _fetchRuns, _registry, _cache, _uow, _clock,
        NullLogger<TriggerFetchCommandHandler>.Instance);

    private static FetchedContent Video(string id) =>
        new(id, "Video " + id, null, ContentType.Video, new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero), null, 1000, 100, null, null);

    [Fact]
    public async Task Handle_NewItems_AreCreated_AndFetchRunSucceeds_AndCacheInvalidated()
    {
        _providers.GetByIdAsync(_provider.Id, Arg.Any<CancellationToken>()).Returns(_provider);
        _content.GetByNaturalKeyAsync(_provider.Id, Arg.Any<ExternalId>(), Arg.Any<CancellationToken>())
            .Returns((ContentItem?)null);
        _registry.Resolve(ProviderFormat.Json).Returns(
            new FakeProviderAdapter(ProviderFormat.Json, new[] { Video("a"), Video("b") }));

        var summary = await CreateHandler().Handle(new TriggerFetchCommand(_provider.Id), CancellationToken.None);

        var result = Assert.Single(summary.Providers);
        Assert.Equal(nameof(FetchRunStatus.Succeeded), result.Status);
        Assert.Equal(2, result.Incoming);
        Assert.Equal(2, result.New);
        Assert.Equal(0, result.Updated);
        await _content.Received(2).AddAsync(Arg.Any<ContentItem>(), Arg.Any<CancellationToken>());
        await _cache.Received(1).InvalidateAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingItem_IsUpdated_NotDuplicated()
    {
        var existing = ContentItem.Create(
            _provider.Id, ExternalId.Create("a"), "Eski", null, ContentType.Video,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), null,
            ContentMetrics.ForVideo(10, 1), _clock.UtcNow);
        _providers.GetByIdAsync(_provider.Id, Arg.Any<CancellationToken>()).Returns(_provider);
        _content.GetByNaturalKeyAsync(_provider.Id, Arg.Any<ExternalId>(), Arg.Any<CancellationToken>())
            .Returns(existing);
        _registry.Resolve(ProviderFormat.Json).Returns(
            new FakeProviderAdapter(ProviderFormat.Json, new[] { Video("a") }));

        var summary = await CreateHandler().Handle(new TriggerFetchCommand(_provider.Id), CancellationToken.None);

        Assert.Equal(0, summary.Providers[0].New);
        Assert.Equal(1, summary.Providers[0].Updated);
        await _content.DidNotReceive().AddAsync(Arg.Any<ContentItem>(), Arg.Any<CancellationToken>());
        _content.Received(1).Update(existing);
    }

    [Fact]
    public async Task Handle_RateLimited_MarksRunRateLimited_AndDoesNotInvalidateCache()
    {
        _providers.GetByIdAsync(_provider.Id, Arg.Any<CancellationToken>()).Returns(_provider);
        _registry.Resolve(ProviderFormat.Json).Returns(new FakeProviderAdapter(
            ProviderFormat.Json,
            Array.Empty<FetchedContent>(),
            new ProviderRateLimitedException("429")));

        var summary = await CreateHandler().Handle(new TriggerFetchCommand(_provider.Id), CancellationToken.None);

        Assert.Equal(nameof(FetchRunStatus.RateLimited), summary.Providers[0].Status);
        await _cache.DidNotReceive().InvalidateAsync(Arg.Any<CancellationToken>());
    }
}
