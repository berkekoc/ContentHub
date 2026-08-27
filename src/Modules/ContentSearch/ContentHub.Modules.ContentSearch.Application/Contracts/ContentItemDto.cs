using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Application.Contracts;

/// <summary>Arama sonucundaki tek temsilci öğe. Dashboard en az Başlık/Tür/Skor gösterir (R10).</summary>
public sealed record ContentItemDto(
    Guid Id,
    string Title,
    string? Description,
    ContentType Type,
    DateTimeOffset PublishedAt,
    decimal FinalScore,
    double Relevance,
    int ProviderCount);
