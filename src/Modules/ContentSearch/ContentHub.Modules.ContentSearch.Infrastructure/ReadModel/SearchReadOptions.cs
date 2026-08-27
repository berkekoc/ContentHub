namespace ContentHub.Modules.ContentSearch.Infrastructure.ReadModel;

/// <summary>Arama okuma yolu yapılandırması (config: "ContentSearch:Search").</summary>
public sealed class SearchReadOptions
{
    public const string SectionName = "ContentSearch:Search";

    /// <summary>FTS config; EF generated column ile BİREBİR aynı olmalı ('simple' varsayılan, Notes 3).</summary>
    public string TextSearchConfig { get; set; } = "simple";

    /// <summary>Hybrid ağırlıkları (Notes 4; v1 sezgisel).</summary>
    public double HybridRelevanceWeight { get; set; } = 0.5;

    public double HybridPopularityWeight { get; set; } = 0.5;

    /// <summary>final_score ile ts_rank ölçek farkını kabaca hizalayan sabit.</summary>
    public double HybridScale { get; set; } = 10.0;
}
