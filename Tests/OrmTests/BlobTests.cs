using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests
{
    public class BlobTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        private readonly TestStormContext _context;

        public BlobTests(DatabaseFixture fixture, ITestOutputHelper output)
        {
            var logger = new XunitLogger<DatabaseFixture>(output);

            StormManager.SetLogger(logger);

            _context = new TestStormContext(fixture.ConnectionString);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await _context.DisposeAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task AllMethodsForDynamicTableShouldWorkFine()
        {
            var (conn, _) = await _context.EnsureConnectionAsync(CancellationToken.None);
            await conn.CreateTableAsync<Blob>(true, unquotedSchemaName: "dbo", unquotedTableName: "BlobTest1");
            const string tableName = "dbo.BlobTest1";

            var r = await _context.SelectFromBlob($"SELECT * from {tableName}").ListAsync();
            Assert.Empty(r);

            await _context.InsertIntoBlob(tableName)
                .Values(new Blob
                {
                    Metadata = "",
                    BigString = "2321311",
                    Id = 1,
                    SomeOtherValue = 1
                })
                .GoAsync();


            var r2 = await _context.SelectFromBlob($"SELECT * from {tableName}").ListAsync();
            Assert.Single(r2);

            var item = r2[0];
            item.StartChangeTracking();

            item.Metadata = "new metadata";
            await _context.UpdateBlob(tableName).Set(item).GoAsync();

            var updated = await _context.SelectFromBlob(item.Id, $"SELECT * from {tableName}").GetAsync();
            Assert.NotNull(updated);
            Assert.Equal("new metadata", updated.Metadata);


            await _context.UpdateBlob(tableName)
                .Set(x => x.Metadata, "some metadata")
                .Where(x => x.Id == item.Id).GoAsync();

            updated = await _context.SelectFromBlob(item.Id, $"SELECT * from {tableName}").GetAsync();
            Assert.NotNull(updated);
            Assert.Equal("some metadata", updated.Metadata);


            var x = _context.UpdateBlob(item.Id, tableName)
                .Set(x => x.Metadata, "New metadata");

            await x.GoAsync();


            updated = await _context.SelectFromBlob(item.Id, $"SELECT * from {tableName}").GetAsync();
            Assert.NotNull(updated);
            Assert.Equal("New metadata", updated.Metadata);
            await _context.DeleteFromBlob(item.Id, tableName).GoAsync();

            updated = await _context.SelectFromBlob(item.Id, $"SELECT * from {tableName}").GetAsync();
            Assert.Null(updated);


            var blobs = Enumerable.Range(1, 100).Select(x => new Blob
            {
                Metadata = $"Some metadata for {x}",
                BigString = $"Some very large string for id {x}",
                Id = 1,
                SomeOtherValue = x
            });

            await _context.BulkInsertIntoBlob(tableName).Values(blobs).GoAsync();

            var count = await _context.SelectFromBlob($"SELECT * from {tableName}").CountAsync();
            Assert.Equal(100, count);
        }
    }
}
