namespace ContentHub.Modules.ContentSearch.Application.Abstractions;

/// <summary>Yazma tarafı kalıcılık sınırı; tek DbContext'in commit noktası.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
