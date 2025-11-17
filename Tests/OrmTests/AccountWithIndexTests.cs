using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using AltaSoft.Storm.TestModels.VeryBadNamespace;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

public class AccountWithIndexTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;

    public AccountWithIndexTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        var logger = new XunitLogger<DatabaseFixture>(output);
        StormManager.SetLogger(logger);

        _context = new TestStormContext(fixture.ConnectionString);
    }

    public Task InitializeAsync() => _context.GetConnection().OpenAsync();

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task InsertAndGetAccountByUniqueIndex()
    {
        var account = new Account
        {
            Id = 1,
            RelatedCustomerId = new CustomerId(1),
            Ccy = "USD",
            IbanAccount = "US12345678901234567890",
            BbanAccount = 1234567890123456,
            BranchId = 1,
            Type = 1,
            Name = "Test Account KA",
            CanDebit = true,
            CanCredit = true
        };
        await _context.InsertIntoAccount().Values(account).GoAsync();

        var retrievedAccount = await _context.SelectFromAccount("US12345678901234567890", "USD")
            .GetAsync();

        Assert.NotNull(retrievedAccount);
        Assert.Equal(account.Id, retrievedAccount.Id);
    }
}
