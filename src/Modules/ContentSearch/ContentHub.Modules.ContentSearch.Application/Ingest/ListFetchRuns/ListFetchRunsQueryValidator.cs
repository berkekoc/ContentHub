using FluentValidation;

namespace ContentHub.Modules.ContentSearch.Application.Ingest.ListFetchRuns;

public sealed class ListFetchRunsQueryValidator : AbstractValidator<ListFetchRunsQuery>
{
    public ListFetchRunsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
