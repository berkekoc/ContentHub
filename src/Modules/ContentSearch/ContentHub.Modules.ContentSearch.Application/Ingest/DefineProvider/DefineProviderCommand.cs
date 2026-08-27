using ContentHub.Modules.ContentSearch.Domain.Model;
using MediatR;

namespace ContentHub.Modules.ContentSearch.Application.Ingest.DefineProvider;

/// <summary>
/// Yeni sağlayıcı tanımla (Operatör op. 8). Çekirdek iş kuralına dokunmadan üçüncü
/// sağlayıcı eklenebilir (Requirements 8) — yalnızca yapılandırma + adaptör kaydı.
/// </summary>
public sealed record DefineProviderCommand(
    string Name,
    ProviderFormat Format,
    string BaseUrl,
    int? RequestsPerMinute,
    OverflowBehavior? OverflowBehavior) : IRequest<Guid>;
