using ContentHub.BuildingBlocks.Application.Models;
using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Application.Ingest.DefineProvider;
using ContentHub.Modules.ContentSearch.Application.Ingest.ListFetchRuns;
using ContentHub.Modules.ContentSearch.Application.Ingest.TriggerFetch;
using ContentHub.Modules.ContentSearch.Endpoints.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ContentHub.Modules.ContentSearch.Endpoints;

/// <summary>Yazma/gözlem uçları — ApiKey korumalı (S8).</summary>
internal static class IngestEndpoints
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api")
            .WithTags("Ingest")
            .RequireAuthorization(ApiKeyPolicy.Name);

        group.MapPost("/fetch", TriggerFetchAsync)
            .WithName("TriggerFetch")
            .WithSummary("Bir ya da tüm sağlayıcılar için çekimi elle tetikler (korumalı).");

        group.MapGet("/fetch-runs", ListFetchRunsAsync)
            .WithName("ListFetchRuns")
            .WithSummary("Çekim çalıştırmalarını listeler (korumalı).");

        group.MapPost("/providers", DefineProviderAsync)
            .WithName("DefineProvider")
            .WithSummary("Yeni sağlayıcı tanımlar (korumalı).");
    }

    private static async Task<Accepted<FetchQueuedResponse>> TriggerFetchAsync(
        IBackgroundTaskQueue queue,
        Guid? providerId,
        CancellationToken cancellationToken)
    {
        // Çekim uzun sürebilir → HTTP isteğini bloklamadan arka plan kuyruğuna al (202 Accepted).
        // İş, tüketicinin açtığı taze DI kapsamında AYNI idempotent TriggerFetchCommand'i çalıştırır.
        // İlerleme GET /api/fetch-runs ile izlenir.
        await queue.EnqueueAsync(
            (serviceProvider, ct) =>
                serviceProvider.GetRequiredService<ISender>().Send(new TriggerFetchCommand(providerId), ct),
            cancellationToken);

        return TypedResults.Accepted(
            "/api/fetch-runs",
            new FetchQueuedResponse("Çekim kuyruğa alındı; ilerleme /api/fetch-runs üzerinden izlenir."));
    }

    private sealed record FetchQueuedResponse(string Message);

    private static async Task<Ok<PagedResult<FetchRunDto>>> ListFetchRunsAsync(
        ISender sender,
        Guid? providerId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new ListFetchRunsQuery(providerId, page, pageSize), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Created<ProviderCreatedResponse>> DefineProviderAsync(
        ISender sender,
        DefineProviderRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            new DefineProviderCommand(
                request.Name,
                request.Format,
                request.BaseUrl,
                request.RequestsPerMinute,
                request.OverflowBehavior),
            cancellationToken);

        return TypedResults.Created($"/api/providers/{id}", new ProviderCreatedResponse(id));
    }

    private sealed record ProviderCreatedResponse(Guid Id);
}
