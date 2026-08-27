using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Persistence;

/// <summary>
/// Modülün tek DbContext'i. Yazma modelinin (aggregate) tutarlılık ve commit noktası
/// (IUnitOfWork). Okuma tarafı bu context'i aggregate izleme için KULLANMAZ — ham SQL
/// projeksiyonu (ISearchReadModel) üzerinden okur (CQRS).
/// </summary>
public sealed class ContentSearchDbContext : DbContext, IUnitOfWork
{
    public const string Schema = "content_search";

    public ContentSearchDbContext(DbContextOptions<ContentSearchDbContext> options)
        : base(options)
    {
    }

    public DbSet<Provider> Providers => Set<Provider>();

    public DbSet<ContentItem> ContentItems => Set<ContentItem>();

    public DbSet<ProviderFetchRun> ProviderFetchRuns => Set<ProviderFetchRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContentSearchDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
