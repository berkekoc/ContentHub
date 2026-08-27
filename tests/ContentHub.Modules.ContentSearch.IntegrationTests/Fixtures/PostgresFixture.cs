using ContentHub.Modules.ContentSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace ContentHub.Modules.ContentSearch.IntegrationTests.Fixtures;

/// <summary>
/// Testcontainers ile gerçek PostgreSQL. Şema modelden EnsureCreated ile kurulur
/// (search_vector generated column + GIN + unique index dahil). Docker gerektirir.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("contenthub")
        .WithUsername("contenthub")
        .WithPassword("contenthub")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public ContentSearchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ContentSearchDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new ContentSearchDbContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class DatabaseCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
