using ContentHub.Modules.ContentSearch.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Persistence.Configurations;

internal sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.ToTable("providers");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(p => p.Format).HasColumnName("format").HasConversion<short>();
        builder.Property(p => p.BaseUrl).HasColumnName("base_url").IsRequired();
        builder.Property(p => p.Status).HasColumnName("status").HasConversion<short>();

        // İstek limiti politikası aynı tabloda (owned, table-splitting).
        builder.OwnsOne(p => p.RateLimitPolicy, rl =>
        {
            rl.Property(x => x.RequestsPerMinute)
                .HasColumnName("rate_limit_per_minute")
                .HasDefaultValue(RateLimitPolicy.DefaultRequestsPerMinute);
            rl.Property(x => x.OverflowBehavior)
                .HasColumnName("overflow_behavior")
                .HasConversion<short>();
        });
        builder.Navigation(p => p.RateLimitPolicy).IsRequired();
    }
}
