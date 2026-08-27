namespace ContentHub.Modules.ContentSearch.Infrastructure.Scheduling;

public sealed class FetchSchedulerOptions
{
    public const string SectionName = "ContentSearch:Scheduler";

    /// <summary>Otomatik tazeleme kapalı gelir; demoda config ile açılır.</summary>
    public bool Enabled { get; set; }

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Uygulama açılışından sonra ilk çekime kadar beklenen süre.</summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(1);
}
