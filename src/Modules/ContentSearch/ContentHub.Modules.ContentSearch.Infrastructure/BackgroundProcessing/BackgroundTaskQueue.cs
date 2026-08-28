using System.Threading.Channels;
using ContentHub.Modules.ContentSearch.Application.Abstractions;

namespace ContentHub.Modules.ContentSearch.Infrastructure.BackgroundProcessing;

/// <summary>
/// <see cref="IBackgroundTaskQueue"/>'nun System.Threading.Channels ile in-process uygulaması.
/// Sınırlı kapasite (varsayılan 100): kuyruk dolarsa üretici, yer açılana kadar bekler (sırt basıncı).
/// Not: in-memory olduğundan süreç yeniden başlarsa BEKLEYEN işler kaybolur — dayanıklılık gerekiyorsa
/// port'un arkasına kalıcı bir broker (RabbitMQ/Hangfire) konur.
/// </summary>
internal sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _channel;

    public BackgroundTaskQueue(int capacity = 100)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
        };
        _channel = Channel.CreateBounded<Func<IServiceProvider, CancellationToken, Task>>(options);
    }

    public async ValueTask EnqueueAsync(
        Func<IServiceProvider, CancellationToken, Task> workItem,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        await _channel.Writer.WriteAsync(workItem, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        => await _channel.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
}
