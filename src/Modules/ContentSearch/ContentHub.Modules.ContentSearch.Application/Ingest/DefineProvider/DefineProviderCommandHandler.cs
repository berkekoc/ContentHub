using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Domain.Model;
using MediatR;

namespace ContentHub.Modules.ContentSearch.Application.Ingest.DefineProvider;

internal sealed class DefineProviderCommandHandler : IRequestHandler<DefineProviderCommand, Guid>
{
    private readonly IProviderRepository _providerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DefineProviderCommandHandler(IProviderRepository providerRepository, IUnitOfWork unitOfWork)
    {
        _providerRepository = providerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(DefineProviderCommand request, CancellationToken cancellationToken)
    {
        var policy = request.RequestsPerMinute is { } rpm
            ? RateLimitPolicy.Create(rpm, request.OverflowBehavior ?? OverflowBehavior.Wait)
            : RateLimitPolicy.Default;

        var provider = Provider.Create(request.Name, request.Format, request.BaseUrl, policy);

        await _providerRepository.AddAsync(provider, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return provider.Id;
    }
}
