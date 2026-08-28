using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Domain.Model;
using ContentHub.Modules.ContentSearch.Infrastructure.Caching;
using ContentHub.Modules.ContentSearch.Infrastructure.Persistence;
using ContentHub.Modules.ContentSearch.Infrastructure.Persistence.Repositories;
using ContentHub.Modules.ContentSearch.Infrastructure.Providers;
using ContentHub.Modules.ContentSearch.Infrastructure.ReadModel;
using ContentHub.Modules.ContentSearch.Infrastructure.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContentHub.Modules.ContentSearch.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "ContentHub";

    /// <summary>
    /// Modülün tüm Infrastructure servislerini bağlar (composition root'tan çağrılır):
    /// persistence, provider entegrasyonu (dayanıklılık + limit), okuma modeli, önbellek, zamanlayıcı.
    /// </summary>
    public static IServiceCollection AddContentSearchInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // --- Options ---
        services.Configure<SearchReadOptions>(configuration.GetSection(SearchReadOptions.SectionName));
        services.Configure<SearchCacheOptions>(configuration.GetSection(SearchCacheOptions.SectionName));
        services.Configure<FetchSchedulerOptions>(configuration.GetSection(FetchSchedulerOptions.SectionName));

        // --- Persistence ---
        var connectionString =
            configuration.GetConnectionString(ConnectionStringName)
            ?? Environment.GetEnvironmentVariable("CONTENTHUB_DB")
            ?? throw new InvalidOperationException(
                $"'{ConnectionStringName}' bağlantı dizesi bulunamadı (ConnectionStrings veya CONTENTHUB_DB).");

        services.AddDbContext<ContentSearchDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", ContentSearchDbContext.Schema)));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ContentSearchDbContext>());
        services.AddScoped<IContentRepository, ContentRepository>();
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IFetchRunRepository, FetchRunRepository>();

        // --- Read model (CQRS okuma yolu) ---
        services.AddScoped<ISearchReadModel, SearchReadModel>();

        // --- Provider entegrasyonu (Ingest) ---
        services.AddSingleton<IOutboundRateLimiter, OutboundRateLimiter>();
        services.AddHttpClient<ProviderHttpClient>()
            .AddStandardResilienceHandler(); // Polly v8: retry (transient/429/5xx) + circuit breaker + timeout
        services.AddTransient<IProviderAdapter, JsonProviderAdapter>();
        services.AddTransient<IProviderAdapter, XmlProviderAdapter>();
        services.AddTransient<IProviderAdapterRegistry, ProviderAdapterRegistry>();

        // --- Cache (sürüm-jetonu geçersizleştirme) ---
        services.AddSingleton<ISearchResultCache, DistributedSearchResultCache>();

        // --- Zamanlanmış çekim ---
        services.AddHostedService<FetchSchedulerBackgroundService>();

        return services;
    }

    /// <summary>
    /// Şemayı modelden oluşturur (yerel/demo kolaylığı). Api katmanı DbContext tipine
    /// DOKUNMADAN bunu çağırır (mimari sınır korunur). Üretimde `dotnet ef database update`
    /// tercih edilir.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ContentSearchDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Yapılandırmadaki (<c>ContentSearch:Providers</c>) sağlayıcıları DB'ye idempotent olarak
    /// ekler — case'in verdiği WEG uçları (provider1 JSON, provider2 XML) demo açılışında hazır olsun.
    /// Ada göre tekrarı önler; DbContext'e Api DOKUNMADAN Infrastructure üstünden çağrılır (sınır korunur).
    /// </summary>
    public static async Task SeedProvidersAsync(this IServiceProvider services, IConfiguration configuration)
    {
        var configured = configuration.GetSection("ContentSearch:Providers").Get<ProviderSeedOptions[]>()
                         ?? Array.Empty<ProviderSeedOptions>();
        if (configured.Length == 0)
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ContentSearchDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("ProviderSeeder");

        var added = 0;
        foreach (var entry in configured)
        {
            if (string.IsNullOrWhiteSpace(entry.Name) || string.IsNullOrWhiteSpace(entry.BaseUrl))
            {
                continue;
            }

            if (!Enum.TryParse<ProviderFormat>(entry.Format, ignoreCase: true, out var format))
            {
                logger.LogWarning("Geçersiz provider biçimi atlandı: {Name} / {Format}", entry.Name, entry.Format);
                continue;
            }

            var exists = await db.Providers.AnyAsync(p => p.Name == entry.Name);
            if (exists)
            {
                continue;
            }

            db.Providers.Add(Provider.Create(entry.Name!, format, entry.BaseUrl!));
            added++;
            logger.LogInformation("Sağlayıcı seed edildi: {Name} ({Format})", entry.Name, format);
        }

        if (added > 0)
        {
            await db.SaveChangesAsync();
        }
    }

    private sealed class ProviderSeedOptions
    {
        public string? Name { get; set; }

        public string? Format { get; set; }

        public string? BaseUrl { get; set; }
    }
}
