using ContentHub.BuildingBlocks.Application.Models;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Application.Search.GetScoreBreakdown;
using ContentHub.Modules.ContentSearch.Application.Search.SearchContent;
using ContentHub.Modules.ContentSearch.Domain.Model;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ContentHub.Modules.ContentSearch.Endpoints;

/// <summary>Okuma uçları — AÇIK (S8). Arama/filtre/sırala/sayfala + skor açıklaması.</summary>
internal static class SearchEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api").WithTags("Search");

        group.MapGet("/search", SearchAsync)
            .WithName("SearchContent")
            .WithSummary("İçerik ara/filtrele/sırala/sayfala (açık).");

        group.MapGet("/content/{id:guid}/score", GetScoreAsync)
            .WithName("GetScoreBreakdown")
            .WithSummary("Bir içeriğin skor bileşenlerini döndürür (bonus).");
    }

    private static async Task<Ok<PagedResult<ContentItemDto>>> SearchAsync(
        ISender sender,
        string? keyword,
        ContentType? type,
        SortOption sort = SortOption.Popularity,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new SearchContentQuery(keyword, type, sort, page, pageSize),
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<ScoreBreakdownDto>> GetScoreAsync(
        ISender sender,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetScoreBreakdownQuery(id), cancellationToken);
        return TypedResults.Ok(result);
    }
}
