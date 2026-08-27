namespace ContentHub.Modules.ContentSearch.Domain.Scoring;

/// <summary>Güncellik aralığı ve karşılık gelen puan (Norms 2).</summary>
public enum RecencyBand
{
    /// <summary>≤ 1 hafta → +5.</summary>
    Week = 5,

    /// <summary>≤ 1 ay → +3.</summary>
    Month = 3,

    /// <summary>≤ 3 ay → +1.</summary>
    Quarter = 1,

    /// <summary>Daha eski → +0.</summary>
    Older = 0,
}
