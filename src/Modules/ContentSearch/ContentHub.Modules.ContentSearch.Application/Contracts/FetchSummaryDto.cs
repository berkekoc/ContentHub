namespace ContentHub.Modules.ContentSearch.Application.Contracts;

/// <summary>Bir çekim tetiklemesinin sağlayıcı bazlı özeti (Operatör operasyonu 6).</summary>
public sealed record FetchSummaryDto(IReadOnlyList<ProviderFetchResultDto> Providers);

public sealed record ProviderFetchResultDto(
    Guid ProviderId,
    string ProviderName,
    string Status,
    int Incoming,
    int New,
    int Updated,
    string? Error);
