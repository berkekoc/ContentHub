namespace ContentHub.Modules.ContentSearch.Application.Abstractions;

/// <summary>
/// Uygulama-içi (in-process) arka plan iş kuyruğu port'u. Manuel çekim gibi uzun süren işler
/// HTTP isteğini BLOKLAMADAN buraya alınır (202 Accepted). Bu, demo/ücretsiz katman için yeterli
/// ve doğru seçimdir; production'da AYNI port'un arkasına RabbitMQ / Hangfire konabilir —
/// çekirdek iş mantığı değişmeden (Clean Architecture: dış dünya adaptör arkasında).
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>Bir iş öğesini kuyruğa yazar. İş, tüketicinin açtığı taze bir DI kapsamında çalışır.</summary>
    ValueTask EnqueueAsync(
        Func<IServiceProvider, CancellationToken, Task> workItem,
        CancellationToken cancellationToken = default);

    /// <summary>Sıradaki iş öğesini bekleyerek alır.</summary>
    ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}
