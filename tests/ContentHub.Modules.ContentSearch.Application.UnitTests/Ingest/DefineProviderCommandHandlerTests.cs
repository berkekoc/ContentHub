using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Ingest.DefineProvider;
using ContentHub.Modules.ContentSearch.Domain.Model;
using NSubstitute;
using Xunit;

namespace ContentHub.Modules.ContentSearch.Application.UnitTests.Ingest;

public sealed class DefineProviderCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesProvider_PersistsAndReturnsId()
    {
        var repo = Substitute.For<IProviderRepository>();
        var uow = Substitute.For<IUnitOfWork>();
        var handler = new DefineProviderCommandHandler(repo, uow);

        var id = await handler.Handle(
            new DefineProviderCommand("XML Kaynak", ProviderFormat.Xml, "https://p/xml", 30, OverflowBehavior.Retry),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        await repo.Received(1).AddAsync(
            Arg.Is<Provider>(p => p.Name == "XML Kaynak" && p.Format == ProviderFormat.Xml && p.RateLimitPolicy.RequestsPerMinute == 30),
            Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
