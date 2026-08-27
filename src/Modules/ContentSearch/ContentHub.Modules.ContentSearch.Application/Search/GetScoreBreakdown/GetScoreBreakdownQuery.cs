using ContentHub.Modules.ContentSearch.Application.Contracts;
using MediatR;

namespace ContentHub.Modules.ContentSearch.Application.Search.GetScoreBreakdown;

/// <summary>Skoru anla (bonus, okuma operasyonu 5): ara bileşenler + o anki güncellik.</summary>
public sealed record GetScoreBreakdownQuery(Guid ContentItemId) : IRequest<ScoreBreakdownDto>;
