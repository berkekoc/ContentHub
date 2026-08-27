using ContentHub.BuildingBlocks.Application.Models;
using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Application.Search.SearchContent;
using ContentHub.Modules.ContentSearch.Application.UnitTests.TestDoubles;
using ContentHub.Modules.ContentSearch.Domain.Model;
using NSubstitute;
using Xunit;

namespace ContentHub.Modules.ContentSearch.Application.UnitTests.Search;

public sealed class SearchContentQueryHandlerTests
{
    private readonly ISearchReadModel _readModel = Substitute.For<ISearchReadModel>();
    private readonly ISearchResultCache _cache = Substitute.For<ISearchResultCache>();
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));

    private SearchContentQueryHandler CreateHandler() => new(_readModel, _cache, _clock);

    private static SearchContentQuery Query() => new("dünya", ContentType.Video, SortOption.Popularity, 1, 20);

    [Fact]
    public async Task Handle_CacheHit_ReturnsCached_DoesNotHitReadModel()
    {
        var cached = new PagedResult<ContentItemDto>(Array.Empty<ContentItemDto>(), 1, 20, 0);
        _cache.GetAsync(Arg.Any<SearchCriteria>(), Arg.Any<CancellationToken>()).Returns(cached);

        var result = await CreateHandler().Handle(Query(), CancellationToken.None);

        Assert.Same(cached, result);
        await _readModel.DidNotReceive().SearchAsync(Arg.Any<SearchCriteria>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheMiss_QueriesReadModel_AndCachesResult()
    {
        _cache.GetAsync(Arg.Any<SearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns((PagedResult<ContentItemDto>?)null);
        var fromDb = new PagedResult<ContentItemDto>(Array.Empty<ContentItemDto>(), 1, 20, 3);
        _readModel.SearchAsync(Arg.Any<SearchCriteria>(), _clock.UtcNow, Arg.Any<CancellationToken>()).Returns(fromDb);

        var result = await CreateHandler().Handle(Query(), CancellationToken.None);

        Assert.Same(fromDb, result);
        await _cache.Received(1).SetAsync(Arg.Any<SearchCriteria>(), fromDb, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BlankKeyword_NormalizedToNullInCriteria()
    {
        _cache.GetAsync(Arg.Any<SearchCriteria>(), Arg.Any<CancellationToken>())
            .Returns((PagedResult<ContentItemDto>?)null);
        _readModel.SearchAsync(Arg.Any<SearchCriteria>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(PagedResult<ContentItemDto>.Empty(1, 20));

        await CreateHandler().Handle(new SearchContentQuery("   ", null, SortOption.Relevance, 1, 20), CancellationToken.None);

        await _readModel.Received(1).SearchAsync(
            Arg.Is<SearchCriteria>(c => c.Keyword == null),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }
}
