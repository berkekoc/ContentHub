using ContentHub.Modules.ContentSearch.Application.Ingest.TriggerFetch;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Scheduling;

/// <summary>
/// Zamanlanmış çekim (Sistem op. 9). PeriodicTimer ile periyodik olarak AYNI idempotent
/// TriggerFetch komutunu çağırır (manuel çekimle aynı akış). Scoped bağımlılıklar için
/// her tetiklemede yeni bir servis kapsamı açılır.
/// </summary>
internal sealed class FetchSchedulerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly FetchSchedulerOptions _options;
    private readonly ILogger<FetchSchedulerBackgroundService> _logger;

    public FetchSchedulerBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<FetchSchedulerOptions> options,
        ILogger<FetchSchedulerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Zamanlanmış çekim kapalı (ContentSearch:Scheduler:Enabled=false).");
            return;
        }

        try
        {
            await Task.Delay(_options.InitialDelay, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(_options.Interval);
        do
        {
            await TriggerAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await SafeWaitAsync(timer, stoppingToken).ConfigureAwait(false));
    }

    private async Task TriggerAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var summary = await sender.Send(new TriggerFetchCommand(null), stoppingToken).ConfigureAwait(false);
            _logger.LogInformation("Zamanlanmış çekim çalıştı: {ProviderCount} sağlayıcı.", summary.Providers.Count);
        }
        catch (OperationCanceledException)
        {
            // kapanış — yut.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Zamanlanmış çekim hata verdi.");
        }
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
