using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Application.Contracts;

/// <summary>"Skor neden bu?" açıklanabilirliği (bonus, R#5). Güncellik okuma anında eklenir.</summary>
public sealed record ScoreBreakdownDto(
    Guid Id,
    string Title,
    ContentType Type,
    decimal BaseScore,
    decimal TypeCoefficient,
    decimal EngagementScore,
    decimal PersistentScore,
    int RecencyPoints,
    decimal FinalScore,
    DateTimeOffset ComputedAt);
