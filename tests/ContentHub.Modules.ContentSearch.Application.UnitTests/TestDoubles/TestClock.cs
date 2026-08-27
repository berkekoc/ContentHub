using ContentHub.BuildingBlocks.Domain.Abstractions;

namespace ContentHub.Modules.ContentSearch.Application.UnitTests.TestDoubles;

internal sealed class TestClock : IClock
{
    public TestClock(DateTimeOffset now) => UtcNow = now;

    public DateTimeOffset UtcNow { get; set; }
}
