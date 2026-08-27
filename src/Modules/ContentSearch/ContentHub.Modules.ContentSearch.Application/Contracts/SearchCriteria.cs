using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Application.Contracts;

/// <summary>Arama okuma modeline ve önbellek anahtarına giren normalleştirilmiş kriter.</summary>
public sealed record SearchCriteria(
    string? Keyword,
    ContentType? ContentType,
    SortOption Sort,
    int Page,
    int PageSize);
