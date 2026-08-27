using ContentHub.Modules.ContentSearch.Application.Abstractions;
using ContentHub.Modules.ContentSearch.Infrastructure.Caching;
using ContentHub.Modules.ContentSearch.Infrastructure.Persistence;
using ContentHub.Modules.ContentSearch.Infrastructure.Persistence.Repositories;
using ContentHub.Modules.ContentSearch.Infrastructure.Providers;
using ContentHub.Modules.ContentSearch.Infrastructure.ReadModel;
using ContentHub.Modules.ContentSearch.Infrastructure.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
}
