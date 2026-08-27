using ContentHub.BuildingBlocks.Infrastructure.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ContentHub.Api.Infrastructure;

/// <summary>Tüm istisnaları RFC 7807 ProblemDetails'e çevirir (tek eşleme kaynağı: ExceptionProblemMapper).</summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var definition = ExceptionProblemMapper.Map(exception);

        if (definition.Status >= 500)
        {
            _logger.LogError(exception, "İşlenmeyen hata: {Title}", definition.Title);
        }

        var problem = new ProblemDetails
        {
            Status = definition.Status,
            Title = definition.Title,
            Detail = definition.Detail,
            Instance = httpContext.Request.Path,
        };

        if (definition.Errors is not null)
        {
            problem.Extensions["errors"] = definition.Errors;
        }

        httpContext.Response.StatusCode = definition.Status;
        await httpContext.Response
            .WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken)
            .ConfigureAwait(false);

        return true;
    }
}
