using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContentHub.Modules.ContentSearch.Infrastructure.Persistence;

/// <summary>
/// `dotnet ef migrations add ...` için tasarım-zamanı fabrikası. Bağlantı dizesi
/// ortam değişkeninden (CONTENTHUB_DB) ya da yerel Docker Compose varsayılanından gelir.
/// EF modeli tümüyle burada yapılandırıldığı için migration doğru şekilde üretilir
/// (search_vector generated column + GIN + unique dahil).
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ContentSearchDbContext>
{
    public ContentSearchDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CONTENTHUB_DB")
            ?? "Host=localhost;Port=5432;Database=contenthub;Username=contenthub;Password=contenthub";

        var options = new DbContextOptionsBuilder<ContentSearchDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", ContentSearchDbContext.Schema))
            .Options;

        return new ContentSearchDbContext(options);
    }
}
