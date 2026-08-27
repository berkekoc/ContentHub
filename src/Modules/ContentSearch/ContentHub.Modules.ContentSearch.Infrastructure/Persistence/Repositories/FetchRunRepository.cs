using ContentHub.BuildingBlocks.Application.Models;
using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Application.Contracts;
using ContentHub.Modules.ContentSearch.Domain.Model;
using ContentHub.Modules.ContentSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Persistence.Repositories;

internal sealed class FetchRunRepository : IFetchRunRepository
{
    private readonly ContentSearchDbContext _db;

    public FetchRunRepository(ContentSearchDbContext db) => _db = db;

    public async Task AddAsync(ProviderFetchRun run, CancellationToken cancellationToken = default)
        => await _db.ProviderFetchRuns.AddAsync(run, cancellationToken).ConfigureAwait(false);

    public void Update(ProviderFetchRun run) => _db.ProviderFetchRuns.Update(run);

    public async Task<PagedResult<FetchRunDto>> ListAsync(
        Guid? providerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ProviderFetchRuns.AsNoTracking();
        if (providerId is { } id)
        {
            query = query.Where(r => r.ProviderId == id);
        }

        var total = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(r => r.StartedAt)
            .ThenByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new FetchRunDto(
                r.Id,
                r.ProviderId,
                r.StartedAt,
                r.FinishedAt,
                r.IncomingCount,
                r.NewCount,
                r.UpdatedCount,
                r.Status.ToString(),
                r.Error))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<FetchRunDto>(items, page, pageSize, total);
    }
}
