#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels.AdventureWorks;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AltaSoft.Storm.Benchmarks;

[MemoryDiagnoser]
public class AdventureWorksSelectBenchmark : IDisposable, IAsyncDisposable
{
    private readonly AdventureWorksContext _context;
    private readonly AdventureWorksStormContext _stormContext;
    private readonly SqlConnection _connection;

    public AdventureWorksSelectBenchmark()
    {
        _context = new AdventureWorksContext();
        _stormContext = new AdventureWorksStormContext(Constants.ConnectionString);
        if (!StormManager.IsInitialized)
        {
            StormManager.Initialize(new MsSqlOrmProvider(), config =>
            {
                config.AddStormContext<AdventureWorksStormContext>(o =>
                {
                    o.UseConnectionString(Constants.ConnectionString);
                    o.UseDefaultSchema("dbo");
                });
            });
        }
        _connection = new SqlConnection(Constants.ConnectionString);
        _connection.Open();
    }

    [Benchmark]
    public async Task SelectManyPersonsEf()
    {
        var persons = await _context.Persons.AsNoTracking().Take(100).ToListAsync();
        if (persons.Count == 0)
            throw new Exception("No persons");
    }

    [Benchmark]
    public async Task SelectManyPersonsDapper()
    {
        var persons = (await _connection.QueryAsync<Person>("SELECT TOP 100 * FROM Person.Person")).AsList();
        if (!persons.Any())
            throw new Exception("No persons");
    }

    [Benchmark]
    public async Task SelectManyPersonsStorm()
    {
        var persons = await _stormContext.SelectFromPerson().Top(100).ListAsync();
        if (persons.Count == 0)
            throw new Exception("No persons");
    }

    [Benchmark]
    public async Task SelectProductEf()
    {
        var product = await _context.Products.AsNoTracking().FirstAsync(p => p.ProductID == 1001);
        if (product.ProductID != 1001)
            throw new Exception("Product not found");
    }

    [Benchmark]
    public async Task SelectProductDapper()
    {
        var product = await _connection.QuerySingleAsync<Product>("SELECT * FROM Production.Product WHERE ProductID = @ProductID", new Product { ProductID = 1001 });
        if (product.ProductID != 1001)
            throw new Exception("Product not found");
    }

    [Benchmark]
    public async Task SelectProductStorm()
    {
        var product = await _stormContext.SelectFromProduct(1001, "TestProduct", "TP-001", Guid.Empty).GetAsync();
        if (product == null || product.ProductID != 1001)
            throw new Exception("Product not found");
    }

    public void Dispose()
    {
        _context.Dispose();
        _stormContext.Dispose();
        _connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _stormContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
