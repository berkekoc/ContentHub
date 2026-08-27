using ContentHub.Modules.ContentSearch.Domain.Model;

namespace ContentHub.Modules.ContentSearch.Endpoints.Contracts;

public sealed record DefineProviderRequest(
    string Name,
    ProviderFormat Format,
    string BaseUrl,
    int? RequestsPerMinute,
    OverflowBehavior? OverflowBehavior);
