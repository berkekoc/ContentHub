using ContentHub.BuildingBlocks.Application.Models;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Domain.Model;
using MediatR;

namespace ContentHub.Modules.ContentSearch.Application.Search.SearchContent;

/// <summary>İçerik ara/filtrele/sırala/sayfala (okuma operasyonları 1–4).</summary>
public sealed record SearchContentQuery(
    string? Keyword,
    ContentType? ContentType,
    SortOption Sort,
    int Page,
    int PageSize) : IRequest<PagedResult<ContentItemDto>>;
