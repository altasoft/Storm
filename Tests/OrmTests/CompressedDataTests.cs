using System;
using System.Text.Json;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using AltaSoft.Storm.TestModels.AdventureWorks;
using Xunit;
using Xunit.Abstractions;

namespace AltaSoft.Storm.Tests;

public class CompressedDataTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly TestStormContext _context;

    public CompressedDataTests(DatabaseFixture fixture, ITestOutputHelper output)
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
    public async Task InsertAndGetValueCompressedJsonAndXml()
    {
        var product = new Product
        {
            ProductID = 1,
            Name = "Mountain Bike",
            ProductNumber = "MB-100",
            MakeFlag = true,
            FinishedGoodsFlag = true,
            Color = "Red",
            SafetyStockLevel = 100,
            ReorderPoint = 50,
            StandardCost = 500.00m,
            ListPrice = 750.00m,
            Size = "M",
            SizeUnitMeasureCode = "CM",
            WeightUnitMeasureCode = "KG",
            Weight = 14.5m,
            DaysToManufacture = 5,
            ProductLine = "R",
            Class = "H",
            Style = "U",
            ProductSubcategoryID = 1,
            ProductModelID = 1,
            SellStartDate = new DateTime(2025, 7, 1),
            SellEndDate = null,
            DiscontinuedDate = null,
            Rowguid = Guid.NewGuid(),
            ModifiedDate = DateTime.UtcNow
        };

        var data = new CompressedData
        {
            Id = Guid.NewGuid(),
            CompressedStringN = "somestringtobecompressed",
            JsonCompressed = product,
            XmlCompressed = product
        };

        await _context.InsertIntoCompressedData().Values(data).GoAsync();
        var result = await _context.SelectFromCompressedData()
            .Where(x => x.Id == data.Id)
            .GetAsync();

        Assert.NotNull(result);
        Assert.Equal(JsonSerializer.Serialize(result.JsonCompressed), JsonSerializer.Serialize(product));
        Assert.Equal(JsonSerializer.Serialize(result.XmlCompressed), JsonSerializer.Serialize(product));
        Assert.Equal("somestringtobecompressed", result.CompressedStringN);
    }
}
