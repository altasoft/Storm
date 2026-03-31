using System;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests
{
    public class DateTimeOffsetTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
    {
        private readonly TestStormContext _context;
        public DateTimeOffsetTests(DatabaseFixture fixture, ITestOutputHelper output)
        {
            var logger = new XunitLogger<DatabaseFixture>(output);

            StormManager.SetLogger(logger);

            _context = new TestStormContext(fixture.ConnectionString);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync() => await _context.DisposeAsync().ConfigureAwait(false);

        [Fact]
        public async Task Should_Persist_And_Retrieve_DateTimeOffset_Correctly()
        {
            var o = new DateTimeOffsetTestClass
            {
                Id = 1,
                NullableOffset = null,
                RequiredOffset = new DateTimeOffset(DateTime.Now)
            };

            await _context.InsertIntoDateTimeOffsetTestClass()
                .Values(o)
                .GoAsync(CancellationToken.None);

            var item = await _context
                .SelectFromDateTimeOffsetTestClass(o.Id)
                .GetAsync(CancellationToken.None);

            Assert.Equal(o.RequiredOffset, item?.RequiredOffset);
            Assert.Equal(o.NullableOffset, item?.NullableOffset);

            var o2 = new DateTimeOffsetTestClass
            {
                Id = 2,
                NullableOffset = DateTimeOffset.Now.AddDays(-19),
                RequiredOffset = new DateTimeOffset(DateTime.Now)
            };

            await _context.InsertIntoDateTimeOffsetTestClass()
                .Values(o2)
                .GoAsync(CancellationToken.None);

            item = await _context
                .SelectFromDateTimeOffsetTestClass(o2.Id)
                .GetAsync(CancellationToken.None);

            Assert.Equal(o2.RequiredOffset, item?.RequiredOffset);
            Assert.Equal(o2.NullableOffset, item?.NullableOffset);
        }
    }
}
