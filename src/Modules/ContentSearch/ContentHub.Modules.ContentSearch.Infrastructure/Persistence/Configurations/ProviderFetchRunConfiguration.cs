using ContentHub.Modules.ContentSearch.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Persistence.Configurations;

internal sealed class ProviderFetchRunConfiguration : IEntityTypeConfiguration<ProviderFetchRun>
{
    public void Configure(EntityTypeBuilder<ProviderFetchRun> builder)
    {
        builder.ToTable("provider_fetch_runs");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.ProviderId).HasColumnName("provider_id");
        builder.Property(r => r.StartedAt).HasColumnName("started_at");
        builder.Property(r => r.FinishedAt).HasColumnName("finished_at");
        builder.Property(r => r.IncomingCount).HasColumnName("incoming_count");
        builder.Property(r => r.NewCount).HasColumnName("new_count");
        builder.Property(r => r.UpdatedCount).HasColumnName("updated_count");
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<short>();
        builder.Property(r => r.Error).HasColumnName("error");

        builder.HasIndex(r => new { r.ProviderId, r.StartedAt })
            .HasDatabaseName("ix_provider_fetch_runs_provider_started")
            .IsDescending(false, true);
    }
}
