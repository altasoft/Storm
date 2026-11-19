using System;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// TimeSpan properties mapped to SQL <c>time</c> columns must compile and round-trip.
/// </summary>
public class TimeSpanEntityTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly string _connectionString;
    private readonly TestStormContext _context;

    public TimeSpanEntityTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        StormManager.SetLogger(new XunitLogger<DatabaseFixture>(output));
        _connectionString = fixture.ConnectionString;
        _context = new TestStormContext(fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, "IF OBJECT_ID('[dbo].[TimeSpanEntities]') IS NOT NULL DROP TABLE [dbo].[TimeSpanEntities]");
        await connection.CreateTableAsync<TimeSpanEntity>(checkNotExists: true, createDetailTables: false);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, "IF OBJECT_ID('[dbo].[TimeSpanEntities]') IS NOT NULL DROP TABLE [dbo].[TimeSpanEntities]");
    }

    [Fact]
    public async Task InsertAndSelect_TimeSpanProperties_ShouldRoundTrip()
    {
        var withValue = new TimeSpanEntity { Id = 1, Duration = new TimeSpan(0, 13, 45, 30, 125), OptionalDuration = TimeSpan.FromMinutes(90) };
        var withNull = new TimeSpanEntity { Id = 2, Duration = TimeSpan.FromSeconds(5), OptionalDuration = null };

        await _context.InsertIntoTimeSpanEntity().Values([withValue, withNull]).GoAsync();

        var loaded1 = await _context.SelectFromTimeSpanEntity(1).GetAsync();
        loaded1.Should().NotBeNull();
        loaded1!.Duration.Should().Be(withValue.Duration);
        loaded1.OptionalDuration.Should().Be(withValue.OptionalDuration);

        var loaded2 = await _context.SelectFromTimeSpanEntity(2).GetAsync();
        loaded2.Should().NotBeNull();
        loaded2!.Duration.Should().Be(withNull.Duration);
        loaded2.OptionalDuration.Should().BeNull();
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}
