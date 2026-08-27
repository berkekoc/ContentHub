using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Search.GetScoreBreakdown;
using ContentHub.Modules.ContentSearch.Application.UnitTests.TestDoubles;
using ContentHub.Modules.ContentSearch.Domain.Model;
using NSubstitute;
using Xunit;

namespace ContentHub.Modules.ContentSearch.Application.UnitTests.Search;

public sealed class GetScoreBreakdownQueryHandlerTests
{
    private readonly IContentRepository _repository = Substitute.For<IContentRepository>();
    private readonly TestClock _clock = new(new DateTimeOffset(2026, 1, 15, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Handle_NotFound_ThrowsKeyNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((ContentItem?)null);
        var handler = new GetScoreBreakdownQueryHandler(_repository, _clock);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.Handle(new GetScoreBreakdownQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Found_AddsRecencyToPersistentScore()
    {
        // persistent 9.0 ; yayın bugüne yakın → güncellik +5 ; final 14.0
        var publishedAt = _clock.UtcNow.AddDays(-1);
        var item = ContentItem.Create(
            Guid.CreateVersion7(),
            ExternalId.Create("x"),
            "Başlık",
            null,
            ContentType.Video,
            publishedAt,
            null,
            ContentMetrics.ForVideo(2000, 300),
            _clock.UtcNow);
        _repository.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new GetScoreBreakdownQueryHandler(_repository, _clock);

        var dto = await handler.Handle(new GetScoreBreakdownQuery(item.Id), CancellationToken.None);

        Assert.Equal(9.0m, dto.PersistentScore);
        Assert.Equal(5, dto.RecencyPoints);
        Assert.Equal(14.0m, dto.FinalScore);
    }
}
