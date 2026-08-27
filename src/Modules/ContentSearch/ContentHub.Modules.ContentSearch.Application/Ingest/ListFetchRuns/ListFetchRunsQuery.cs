using ContentHub.BuildingBlocks.Application.Models;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using MediatR;

namespace ContentHub.Modules.ContentSearch.Application.Ingest.ListFetchRuns;

/// <summary>Çekim çalıştırmalarını gözlemle (Operatör op. 7).</summary>
public sealed record ListFetchRunsQuery(Guid? ProviderId, int Page, int PageSize)
    : IRequest<PagedResult<FetchRunDto>>;
