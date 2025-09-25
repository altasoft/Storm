using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

public class UsersBulkCopyTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;
    private readonly List<UserBulkCopy> _users;

    public UsersBulkCopyTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        _users = fixture.UsersBulkCopy;
        var logger = new XunitLogger<DatabaseFixture>(output);
        StormManager.SetLogger(logger);

        _context = new TestStormContext(fixture.ConnectionString);
    }

    public Task InitializeAsync() => _context.GetConnection().OpenAsync();

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync().ConfigureAwait(false);
    }

    //[Fact]
    //public async Task BulkCopyEnumerable_ShouldInsertInTable()
    //{
    //    var rows = await _context.BulkInsertIntoUsersBulkCopy()
    //        .WithBatchSize(100)
    //        .WithCommandTimeOut(30)
    //        .Values(_users.Select(x => x with { UserId = x.UserId + 1_000_000 })) // to avoid PK conflict
    //        .GoAsync(CancellationToken.None);

    //    Assert.Equal(_users.Count, rows);

    //    var items = await _context.SelectFromUsersBulkCopy().ListAsync();
    //    Assert.Equal(_users.Count, items.Count);
    //}

    [Fact]
    public async Task BulkCopyChannel_ShouldInsertInTable()
    {
        var channel = Channel.CreateBounded<UserBulkCopy>(1000);
        var reader = channel.Reader;

        for (var index = 0; index < _users.Count / 2; index++)
        {
            var userBulkCopy = _users[index];
            await channel.Writer.WriteAsync(userBulkCopy);
        }

        var readFunc = _context.BulkInsertIntoUsersBulkCopy()
            .WithBatchSize(100)
            .WithCommandTimeOut(30)
            .WithProgressNotification(10, ReportProgress)
            .Values(reader)
            .GoAsync(CancellationToken.None);

        for (var index = _users.Count / 2; index < _users.Count; index++)
        {
            var userBulkCopy = _users[index];
            await channel.Writer.WriteAsync(userBulkCopy);
        }

        channel.Writer.Complete();

        var rows = await readFunc;
        Assert.Equal(_users.Count, rows);

        var items = await _context.SelectFromUsersBulkCopy().ListAsync();
        Assert.Equal(_users.Count, items.Count);
    }

    private void ReportProgress(long progress)
    {
        Console.Write(progress);
    }
}
