using ContentHub.BuildingBlocks.Domain;

namespace ContentHub.Modules.ContentSearch.Domain.Model;

/// <summary>
/// Bir çekim işinin denetim kaydı — "kalıcı veri tutarlığı" iddiasının kanıtı ve
/// gözlemlenebilirliğin kaynağı (S7). Manuel ve zamanlanmış çekim aynı akışı kullanır.
/// </summary>
public sealed class ProviderFetchRun : AggregateRoot<Guid>
{
    private ProviderFetchRun()
    {
    }

    private ProviderFetchRun(Guid id, Guid providerId, DateTimeOffset startedAt)
        : base(id)
    {
        ProviderId = providerId;
        StartedAt = startedAt;
        Status = FetchRunStatus.Running;
    }

    public Guid ProviderId { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public int IncomingCount { get; private set; }

    public int NewCount { get; private set; }

    public int UpdatedCount { get; private set; }

    public FetchRunStatus Status { get; private set; }

    public string? Error { get; private set; }

    public static ProviderFetchRun Start(Guid providerId, DateTimeOffset startedAt)
    {
        if (providerId == Guid.Empty)
        {
            throw new DomainException("Çekim çalıştırması bir sağlayıcıya ait olmalıdır.");
        }

        return new ProviderFetchRun(Guid.CreateVersion7(), providerId, startedAt);
    }

    public void Succeed(DateTimeOffset finishedAt, int incomingCount, int newCount, int updatedCount)
    {
        EnsureRunning();
        FinishedAt = finishedAt;
        IncomingCount = incomingCount;
        NewCount = newCount;
        UpdatedCount = updatedCount;
        Status = FetchRunStatus.Succeeded;
    }

    public void Fail(DateTimeOffset finishedAt, string error)
    {
        EnsureRunning();
        FinishedAt = finishedAt;
        Error = error;
        Status = FetchRunStatus.Failed;
    }

    public void MarkRateLimited(DateTimeOffset finishedAt, string error)
    {
        EnsureRunning();
        FinishedAt = finishedAt;
        Error = error;
        Status = FetchRunStatus.RateLimited;
    }

    private void EnsureRunning()
    {
        if (Status != FetchRunStatus.Running)
        {
            throw new DomainException("Tamamlanmış bir çekim çalıştırması yeniden sonuçlandırılamaz.");
        }
    }
}
