using ContentHub.BuildingBlocks.Application.Models;
using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using MediatR;

namespace ContentHub.Modules.ContentSearch.Application.Ingest.ListFetchRuns;

internal sealed class ListFetchRunsQueryHandler
    : IRequestHandler<ListFetchRunsQuery, PagedResult<FetchRunDto>>
{
    private readonly IFetchRunRepository _fetchRunRepository;

    public ListFetchRunsQueryHandler(IFetchRunRepository fetchRunRepository)
        => _fetchRunRepository = fetchRunRepository;

    public Task<PagedResult<FetchRunDto>> Handle(ListFetchRunsQuery request, CancellationToken cancellationToken)
        => _fetchRunRepository.ListAsync(request.ProviderId, request.Page, request.PageSize, cancellationToken);
}
