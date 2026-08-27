using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Application.Ingest.DefineProvider;
using ContentHub.Modules.ContentSearch.Application.Search.SearchContent;
using ContentHub.Modules.ContentSearch.Domain.Model;
using Xunit;

namespace ContentHub.Modules.ContentSearch.Application.UnitTests.Validators;

public sealed class ValidatorTests
{
    [Theory]
    [InlineData(0, 20, false)]   // page < 1
    [InlineData(1, 0, false)]    // pageSize < 1
    [InlineData(1, 101, false)]  // pageSize > 100
    [InlineData(1, 20, true)]    // geçerli
    public void SearchContentQueryValidator_EnforcesPaging(int page, int pageSize, bool expectedValid)
    {
        var validator = new SearchContentQueryValidator();
        var result = validator.Validate(new SearchContentQuery("x", ContentType.Video, SortOption.Popularity, page, pageSize));
        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData("https://ok/x", true)]
    [InlineData("not-a-url", false)]
    [InlineData("ftp://x/y", false)]
    public void DefineProviderCommandValidator_ValidatesUrl(string url, bool expectedValid)
    {
        var validator = new DefineProviderCommandValidator();
        var result = validator.Validate(new DefineProviderCommand("N", ProviderFormat.Json, url, null, null));
        Assert.Equal(expectedValid, result.IsValid);
    }
}
