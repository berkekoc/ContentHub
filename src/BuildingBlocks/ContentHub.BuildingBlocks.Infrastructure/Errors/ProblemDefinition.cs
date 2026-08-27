namespace ContentHub.BuildingBlocks.Infrastructure.Errors;

/// <summary>
/// Çerçeveden bağımsız hata tanımı. Api katmanı bunu RFC 7807 ProblemDetails'e
/// çevirir; böylece eşleme kuralı ortak altyapıda merkezîleşir ama AspNetCore
/// bağımlılığı buraya sızmaz.
/// </summary>
public sealed record ProblemDefinition(
    int Status,
    string Title,
    string? Detail = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);
