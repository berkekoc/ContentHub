using ContentHub.Modules.ContentSearch.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContentHub.Modules.ContentSearch.Infrastructure.BackgroundProcessing;

/// <summary>
/// Kuyruğu sürekli tüketen arka plan servisi. Her iş öğesini TAZE bir DI kapsamında çalıştırır
/// (scoped bağımlılıklar — DbContext, handler'lar — için). Bir iş hata verirse loglanır; servis düşmez.
/// </summary>
internal sealed class QueuedHostedService : BackgroundService
{
    private readonly IBackgroundTaskQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QueuedHostedService> _logger;

    public QueuedHostedService(
        IBackgroundTaskQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<QueuedHostedService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Arka plan iş kuyruğu tüketicisi başladı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            Func<IServiceProvider, CancellationToken, Task> workItem;
            try
            {
                workItem = await _queue.DequeueAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                await workItem(scope.ServiceProvider, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // kapanış — yut.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Arka plan iş öğesi hata verdi.");
            }
        }
    }
}
