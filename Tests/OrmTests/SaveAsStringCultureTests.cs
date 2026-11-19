using System;
using System.Globalization;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// Values saved with SaveAs.String must round-trip regardless of the current culture
/// (they are written and read using the invariant culture).
/// </summary>
public class SaveAsStringCultureTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly string _connectionString;
    private readonly TestStormContext _context;

    public SaveAsStringCultureTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        var logger = new XunitLogger<DatabaseFixture>(output);
        StormManager.SetLogger(logger);
        _connectionString = fixture.ConnectionString;
        _context = new TestStormContext(fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, "IF OBJECT_ID('[dbo].[SaveAsStringEntities]') IS NOT NULL DROP TABLE [dbo].[SaveAsStringEntities]");
        await connection.CreateTableAsync<SaveAsStringEntity>(checkNotExists: true, createDetailTables: false);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, "IF OBJECT_ID('[dbo].[SaveAsStringEntities]') IS NOT NULL DROP TABLE [dbo].[SaveAsStringEntities]");
    }

    [Theory]
    [InlineData("ka-GE")]
    [InlineData("de-DE")]
    public async Task InsertAndSelect_InNonInvariantCulture_ShouldRoundTrip(string cultureName)
    {
        var entity = new SaveAsStringEntity
        {
            Id = 1,
            Amount = 1.5m,
            NullableAmount = -1234.25m,
            Ratio = 0.125,
            When = new DateTime(2024, 3, 15, 10, 20, 30, DateTimeKind.Utc).AddTicks(1234567),
            WhenOffset = new DateTimeOffset(2024, 3, 15, 10, 20, 30, TimeSpan.FromHours(4)).AddTicks(7654321),
            Day = new DateOnly(2024, 12, 31),
            Time = new TimeOnly(23, 59, 58).Add(TimeSpan.FromTicks(1234567)),
            OwnerId = 1234567
        };

        var savedCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            await _context.InsertIntoSaveAsStringEntity().Values(entity).GoAsync();

            var loaded = await _context.SelectFromSaveAsStringEntity(1).GetAsync();

            loaded.Should().NotBeNull();
            loaded!.Amount.Should().Be(1.5m);
            loaded.NullableAmount.Should().Be(-1234.25m);
            loaded.Ratio.Should().Be(0.125);
            loaded.When.Should().Be(entity.When);
            loaded.When.Kind.Should().Be(DateTimeKind.Utc);
            loaded.WhenOffset.Should().Be(entity.WhenOffset);
            loaded.WhenOffset.Offset.Should().Be(TimeSpan.FromHours(4));
            loaded.Day.Should().Be(entity.Day);
            loaded.Time.Should().Be(entity.Time);
            loaded.OwnerId.Should().Be(entity.OwnerId);
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCulture;
        }

        // The stored text must be culture independent
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT [Amount] FROM [dbo].[SaveAsStringEntities] WHERE [Id] = 1";
        (await cmd.ExecuteScalarAsync()).Should().Be("1.5");
    }

    [Theory]
    [InlineData("ka-GE")]
    [InlineData("de-DE")]
    public async Task Where_OnSaveAsStringColumn_InNonInvariantCulture_ShouldMatchStoredValue(string cultureName)
    {
        var entity = new SaveAsStringEntity
        {
            Id = 2,
            Amount = 2.75m,
            Ratio = 0.5,
            When = new DateTime(2024, 3, 15, 10, 20, 30, DateTimeKind.Utc),
            WhenOffset = new DateTimeOffset(2024, 3, 15, 10, 20, 30, TimeSpan.Zero),
            Day = new DateOnly(2024, 12, 31),
            Time = new TimeOnly(12, 0),
            OwnerId = 42
        };

        var savedCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

            await _context.InsertIntoSaveAsStringEntity().Values(entity).GoAsync();

            var amount = 2.75m;
            var byAmount = await _context.SelectFromSaveAsStringEntity().Where(x => x.Amount == amount).ListAsync();
            byAmount.Should().ContainSingle(x => x.Id == 2);

            var day = new DateOnly(2024, 12, 31);
            var byDay = await _context.SelectFromSaveAsStringEntity().Where(x => x.Day == day).ListAsync();
            byDay.Should().ContainSingle(x => x.Id == 2);
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCulture;
        }
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}
