using ContentHub.BuildingBlocks.Domain.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Domain.Scoring;
using MediatR;

namespace ContentHub.Modules.ContentSearch.Application.Search.GetScoreBreakdown;

internal sealed class GetScoreBreakdownQueryHandler
    : IRequestHandler<GetScoreBreakdownQuery, ScoreBreakdownDto>
{
    private readonly IContentRepository _contentRepository;
    private readonly IClock _clock;

    public GetScoreBreakdownQueryHandler(IContentRepository contentRepository, IClock clock)
    {
        _contentRepository = contentRepository;
        _clock = clock;
    }

    public async Task<ScoreBreakdownDto> Handle(
        GetScoreBreakdownQuery request,
        CancellationToken cancellationToken)
    {
        var item = await _contentRepository
            .GetByIdAsync(request.ContentItemId, cancellationToken)
            .ConfigureAwait(false);

        if (item is null)
        {
            throw new KeyNotFoundException($"İçerik bulunamadı: {request.ContentItemId}");
        }

        // Güncellik puanı okuma anında; C# tek doğruluk kaynağı (ScoringService) ile hesaplanır.
        var recency = ScoringService.RecencyPoints(item.PublishedAt, _clock.UtcNow);
        var finalScore = item.Score.PersistentScore + recency;

        return new ScoreBreakdownDto(
            item.Id,
            item.Title,
            item.Type,
            item.Score.BaseScore,
            item.Score.TypeCoefficient,
            item.Score.EngagementScore,
            item.Score.PersistentScore,
            recency,
            finalScore,
            item.Score.ComputedAt);
    }
}
