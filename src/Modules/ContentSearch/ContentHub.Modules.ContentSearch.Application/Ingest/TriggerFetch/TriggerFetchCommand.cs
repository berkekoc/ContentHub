using ContentHub.Modules.ContentSearch.Application.Contracts;
using MediatR;

namespace ContentHub.Modules.ContentSearch.Application.Ingest.TriggerFetch;

/// <summary>
/// Çekimi elle/otomatik tetikle (Operatör op. 6, Sistem op. 9). ProviderId null ise
/// tüm ETKİN sağlayıcılar çekilir. Manuel ve zamanlanmış çekim AYNI idempotent akışı kullanır.
/// </summary>
public sealed record TriggerFetchCommand(Guid? ProviderId) : IRequest<FetchSummaryDto>;
