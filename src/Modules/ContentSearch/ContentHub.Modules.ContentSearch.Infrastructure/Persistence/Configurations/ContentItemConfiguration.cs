using ContentHub.Modules.ContentSearch.Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Persistence.Configurations;

internal sealed class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    // FTS yapılandırması — okuma modeli (websearch_to_tsquery/ts_rank) ile BİREBİR aynı olmalı.
    public const string TextSearchConfig = "simple";

    public void Configure(EntityTypeBuilder<ContentItem> builder)
    {
        builder.ToTable("content_items");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.ProviderId).HasColumnName("provider_id");

        builder.Property(c => c.ExternalId)
            .HasColumnName("external_id")
            .HasConversion(v => v.Value, v => ExternalId.Create(v))
            .IsRequired();

        builder.Property(c => c.Title).HasColumnName("title").IsRequired();
        builder.Property(c => c.Description).HasColumnName("description");
        builder.Property(c => c.Type).HasColumnName("content_type").HasConversion<short>();
        builder.Property(c => c.PublishedAt).HasColumnName("published_at");
        builder.Property(c => c.SourceUrl).HasColumnName("source_url");

        builder.Property(c => c.Fingerprint)
            .HasColumnName("fingerprint")
            .HasConversion(v => v.Value, v => Fingerprint.FromHash(v))
            .IsRequired();

        // Ham ölçütler → content_metrics (owned, 1—1 ayrı tablo). Tür-dışı alanlar null.
        builder.OwnsOne(c => c.Metrics, m =>
        {
            m.ToTable("content_metrics");
            m.WithOwner().HasForeignKey("ContentItemId");
            m.HasKey("ContentItemId");
            m.Property<Guid>("ContentItemId").HasColumnName("content_item_id");
            m.Property(x => x.Views).HasColumnName("views");
            m.Property(x => x.Likes).HasColumnName("likes");
            m.Property(x => x.ReadingTime).HasColumnName("reading_time");
            m.Property(x => x.Reactions).HasColumnName("reactions");
        });
        builder.Navigation(c => c.Metrics).IsRequired();

        // Saklanan skor bileşenleri → content_scores (owned, 1—1). Güncellik burada YOK (S1).
        builder.OwnsOne(c => c.Score, s =>
        {
            s.ToTable("content_scores");
            s.WithOwner().HasForeignKey("ContentItemId");
            s.HasKey("ContentItemId");
            s.Property<Guid>("ContentItemId").HasColumnName("content_item_id");
            s.Property(x => x.BaseScore).HasColumnName("base_score").HasColumnType("numeric");
            s.Property(x => x.TypeCoefficient).HasColumnName("type_coefficient").HasColumnType("numeric");
            s.Property(x => x.EngagementScore).HasColumnName("engagement_score").HasColumnType("numeric");
            s.Property(x => x.PersistentScore).HasColumnName("persistent_score").HasColumnType("numeric");
            s.Property(x => x.ComputedAt).HasColumnName("computed_at");
            s.HasIndex(x => x.PersistentScore).HasDatabaseName("ix_content_scores_persistent_score");
        });
        builder.Navigation(c => c.Score).IsRequired();

        // Doğal anahtar → idempotency (Norms 9, S/Safety).
        builder.HasIndex(c => new { c.ProviderId, c.ExternalId })
            .IsUnique()
            .HasDatabaseName("ux_content_items_provider_external");

        builder.HasIndex(c => c.Type).HasDatabaseName("ix_content_items_content_type");
        builder.HasIndex(c => c.Fingerprint).HasDatabaseName("ix_content_items_fingerprint");

        // search_vector: STORED generated tsvector (gölge özellik — domain'e sızmaz).
        builder.Property<NpgsqlTsVector>("SearchVector")
            .HasColumnName("search_vector")
            .HasComputedColumnSql(
                $"to_tsvector('{TextSearchConfig}', coalesce(title,'') || ' ' || coalesce(description,''))",
                stored: true);

        builder.HasIndex("SearchVector")
            .HasMethod("gin")
            .HasDatabaseName("ix_content_items_search_vector");
    }
}
