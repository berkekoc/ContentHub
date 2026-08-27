using FluentValidation;

namespace ContentHub.Modules.ContentSearch.Application.Search.SearchContent;

public sealed class SearchContentQueryValidator : AbstractValidator<SearchContentQuery>
{
    public const int MaxPageSize = 100;
    public const int MaxKeywordLength = 200;

    public SearchContentQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Sayfa numarası 1 veya daha büyük olmalıdır.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .WithMessage($"Sayfa boyutu 1 ile {MaxPageSize} arasında olmalıdır.");

        RuleFor(x => x.Keyword)
            .MaximumLength(MaxKeywordLength)
            .When(x => x.Keyword is not null);

        RuleFor(x => x.Sort).IsInEnum();

        RuleFor(x => x.ContentType)
            .IsInEnum()
            .When(x => x.ContentType is not null);
    }
}
