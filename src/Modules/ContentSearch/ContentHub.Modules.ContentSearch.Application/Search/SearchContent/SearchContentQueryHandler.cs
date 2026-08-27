using ContentHub.BuildingBlocks.Application.Models;
using ContentHub.BuildingBlocks.Domain.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using MediatR;

namespace ContentHub.Modules.ContentSearch.Application.Search.SearchContent;

internal sealed class SearchContentQueryHandler
    : IRequestHandler<SearchContentQuery, PagedResult<ContentItemDto>>
{
    private readonly ISearchReadModel _readModel;
    private readonly ISearchResultCache _cache;
    private readonly IClock _clock;

    public SearchContentQueryHandler(
        ISearchReadModel readModel,
        ISearchResultCache cache,
        IClock clock)
    {
        _readModel = readModel;
        _cache = cache;
        _clock = clock;
    }

    public async Task<PagedResult<ContentItemDto>> Handle(
        SearchContentQuery request,
        CancellationToken cancellationToken)
    {
        var criteria = new SearchCriteria(
            NormalizeKeyword(request.Keyword),
            request.ContentType,
            request.Sort,
            request.Page,
            request.PageSize);

        var cached = await _cache.GetAsync(criteria, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var result = await _readModel.SearchAsync(criteria, _clock.UtcNow, cancellationToken).ConfigureAwait(false);
        await _cache.SetAsync(criteria, result, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private static string? NormalizeKeyword(string? keyword)
        => string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
}
