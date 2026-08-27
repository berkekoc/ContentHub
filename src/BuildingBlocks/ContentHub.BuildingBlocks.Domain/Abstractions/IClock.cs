namespace ContentHub.BuildingBlocks.Domain.Abstractions;

/// <summary>
/// Zaman kaynağı soyutlaması. Alan katmanında yalnızca arayüz durur; hiçbir
/// alan hizmeti (özellikle ScoringService) buna referans veremez — zaman
/// her zaman parametre olarak geçer (ArchTest ile zorlanır).
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
