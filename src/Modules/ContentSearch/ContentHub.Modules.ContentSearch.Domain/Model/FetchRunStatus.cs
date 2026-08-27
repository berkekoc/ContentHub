namespace ContentHub.Modules.ContentSearch.Domain.Model;

/// <summary>Çekim çalıştırmasının sonucu (gözlemlenebilirlik, S7).</summary>
public enum FetchRunStatus
{
    Running = 0,
    Succeeded = 1,
    Failed = 2,
    RateLimited = 3,
}
