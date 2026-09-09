using System.Collections.Generic;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using FluentAssertions;
using Xunit;

namespace AltaSoft.Storm.Tests;

/// <summary>
/// End-to-end (real database) verification that DELETE ... Top(n) actually
/// limits the number of deleted rows per call, instead of deleting every
/// row that matches the WHERE clause.
/// </summary>
public class DeleteTopIntegrationTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private const int BatchSize = 10;
    private const int TotalUsers = 100;
    private const int FirstUserId = 1000;

    private readonly TestStormContext _context;

    public DeleteTopIntegrationTests(DatabaseFixture fixture)
    {
        _context = new TestStormContext(fixture.ConnectionString);
    }

    public async Task InitializeAsync()
    {
        // Inserted in small batches - a single 100-row batch with all of User's
        // detail-table columns (Cars, Dates, lists, ...) exceeds SQL Server's
        // 2100-parameter-per-request limit.
        const int insertBatchSize = 10;
        for (var batchStart = 0; batchStart < TotalUsers; batchStart += insertBatchSize)
        {
            var batch = new List<User>(insertBatchSize);
            for (var i = batchStart; i < batchStart + insertBatchSize; i++)
            {
                batch.Add(DatabaseHelper.NewUser(FirstUserId + i));
            }

            await _context.InsertIntoUsersTable().Values(batch).GoAsync();
        }
    }

    [Fact]
    public async Task DeleteWithTop_RemovesExactlyTopRowsPerCall_UntilNoneLeft()
    {
        // Sanity check: all 100 rows were inserted
        var remainingBeforeDelete = await _context.SelectFromUsersTable()
            .Where(x => x.UserId >= FirstUserId)
            .ListAsync();
        remainingBeforeDelete.Should().HaveCount(TotalUsers);

        var iterations = 0;
        var totalDeleted = 0;

        while (true)
        {
            var deletedCount = await _context.DeleteFromUsersTable()
                .Where(x => x.UserId >= FirstUserId)
                .Top(BatchSize)
                .GoAsync();

            if (deletedCount == 0)
                break;

            // Every call must delete exactly BatchSize rows, never more.
            deletedCount.Should().Be(BatchSize);

            iterations++;
            totalDeleted += deletedCount;
        }

        iterations.Should().Be(TotalUsers / BatchSize);
        totalDeleted.Should().Be(TotalUsers);

        var remainingAfterDelete = await _context.SelectFromUsersTable()
            .Where(x => x.UserId >= FirstUserId)
            .ListAsync();
        remainingAfterDelete.Should().BeEmpty();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync().ConfigureAwait(false);
    }
}
