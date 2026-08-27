using ContentHub.Modules.ContentSearch.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace ContentHub.Modules.ContentSearch.IntegrationTests;

/// <summary>Şema kanıtı (O5 DoD): doğal-anahtar unique index, GIN index ve generated tsvector.</summary>
[Collection(DatabaseCollection.Name)]
public sealed class PersistenceSchemaTests
{
    private readonly PostgresFixture _fixture;

    public PersistenceSchemaTests(PostgresFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Schema_HasUniqueNaturalKey_AndGinIndex()
    {
        await using var context = _fixture.CreateContext();
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            var indexes = new List<string>();
            await using (var command = new NpgsqlCommand(
                "SELECT indexname FROM pg_indexes WHERE schemaname = 'content_search'", connection))
            await using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    indexes.Add(reader.GetString(0));
                }
            }

            Assert.Contains("ux_content_items_provider_external", indexes);
            Assert.Contains("ix_content_items_search_vector", indexes);

            // GIN erişim yöntemi doğrulaması.
            await using var amCommand = new NpgsqlCommand(
                @"SELECT am.amname FROM pg_class c
                  JOIN pg_index i ON i.indexrelid = c.oid
                  JOIN pg_am am ON am.oid = c.relam
                  WHERE c.relname = 'ix_content_items_search_vector'", connection);
            var method = (string?)await amCommand.ExecuteScalarAsync();
            Assert.Equal("gin", method);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    [Fact]
    public async Task SearchVector_IsGeneratedColumn()
    {
        await using var context = _fixture.CreateContext();
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            await using var command = new NpgsqlCommand(
                @"SELECT is_generated FROM information_schema.columns
                  WHERE table_schema = 'content_search' AND table_name = 'content_items' AND column_name = 'search_vector'",
                connection);
            var isGenerated = (string?)await command.ExecuteScalarAsync();
            Assert.Equal("ALWAYS", isGenerated);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
