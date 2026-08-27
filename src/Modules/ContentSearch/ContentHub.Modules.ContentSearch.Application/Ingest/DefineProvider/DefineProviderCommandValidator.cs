using FluentValidation;

namespace ContentHub.Modules.ContentSearch.Application.Ingest.DefineProvider;

public sealed class DefineProviderCommandValidator : AbstractValidator<DefineProviderCommand>
{
    public DefineProviderCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Format).IsInEnum();

        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .Must(BeAValidAbsoluteUrl)
            .WithMessage("Erişim adresi geçerli bir mutlak URL olmalıdır.");

        RuleFor(x => x.RequestsPerMinute!.Value)
            .GreaterThan(0)
            .When(x => x.RequestsPerMinute is not null);

        RuleFor(x => x.OverflowBehavior!.Value)
            .IsInEnum()
            .When(x => x.OverflowBehavior is not null);
    }

    private static bool BeAValidAbsoluteUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
