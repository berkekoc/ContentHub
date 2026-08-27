using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ContentHub.BuildingBlocks.Application.Behaviors;

/// <summary>İstek/yanıt sınırında yapılandırılmış günlükleme ve süre ölçümü.</summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("İstek işleniyor: {RequestName}", requestName);
        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();
            _logger.LogInformation(
                "İstek tamamlandı: {RequestName} ({ElapsedMs} ms)",
                requestName,
                stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "İstek hata verdi: {RequestName} ({ElapsedMs} ms)",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
