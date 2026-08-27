namespace ContentHub.Modules.ContentSearch.Application.Contracts;

/// <summary>Çekim çalıştırması denetim kaydı görünümü (Operatör operasyonu 7).</summary>
public sealed record FetchRunDto(
    Guid Id,
    Guid ProviderId,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    int IncomingCount,
    int NewCount,
    int UpdatedCount,
    string Status,
    string? Error);
