using ContentHub.Modules.ContentSearch.Domain.Scoring;
using ContentHub.Modules.ContentSearch.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using Xunit;

namespace ContentHub.Modules.ContentSearch.IntegrationTests;

/// <summary>
/// İHLAL-EDİLEMEZ (S/Safety): C# ScoringService.RecencyPoints ile okuma SQL'indeki güncellik
/// CASE ifadesi, tam sınır tarihleri için BİREBİR aynı değeri üretmelidir. Bu test kırmızıysa
/// okuma-yazma skor tutarlılığı bozulmuştur (takvim ayı vs 30 gün tuzağı).
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class RecencyParitySqlTests
{
    private readonly PostgresFixture _fixture;

    public RecencyParitySqlTests(PostgresFixture fixture) => _fixture = fixture;

    public static TheoryData<DateTimeOffset> Boundaries()
    {
        var now = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        return new TheoryData<DateTimeOffset>
        {
            now,
            now.AddDays(-7),
            now.AddDays(-7).AddSeconds(-1),
            now.AddMonths(-1),
            now.AddMonths(-1).AddSeconds(-1),
            now.AddMonths(-3),
            now.AddMonths(-3).AddSeconds(-1),
            now.AddMonths(-6),
        };
    }

    [Theory]
    [MemberData(nameof(Boundaries))]
    public async Task SqlRecencyCase_MatchesCsharpRecencyPoints(DateTimeOffset publishedAt)
    {
        var now = new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var expected = ScoringService.RecencyPoints(publishedAt, now);

        await using var context = _fixture.CreateContext();
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            const string sql =
                @"SELECT CASE
                            WHEN @p >= @now - interval '7 days'  THEN 5
                            WHEN @p >= @now - interval '1 month' THEN 3
                            WHEN @p >= @now - interval '3 months' THEN 1
                            ELSE 0
                         END";
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.Add(new NpgsqlParameter("p", NpgsqlDbType.TimestampTz) { Value = publishedAt });
            command.Parameters.Add(new NpgsqlParameter("now", NpgsqlDbType.TimestampTz) { Value = now });
            var sqlValue = Convert.ToInt32(await command.ExecuteScalarAsync());

            Assert.Equal(expected, sqlValue);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
