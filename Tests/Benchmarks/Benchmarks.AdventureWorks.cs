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
public class AdventureWorksBenchmark : IDisposable, IAsyncDisposable
{
    private readonly AdventureWorksContext _context;

    private readonly AdventureWorksStormContext _stormContext;

    private readonly SqlConnection _connection;

    public AdventureWorksBenchmark()
    {
        // For EF Core
        _context = new AdventureWorksContext();

        // For Storm
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

        // For dapper
        _connection = new SqlConnection(Constants.ConnectionString);
        _connection.Open();
    }

    [Benchmark]
    public async Task UpdatePersonEf()
    {
        var person = await _context.Persons.FirstAsync(p => p.BusinessEntityID == 1001);
        person.FirstName = "UpdatedEf";
        await _context.SaveChangesAsync();
    }

    [Benchmark]
    public async Task UpdatePersonDapper()
    {
        // Retrieve the person first, just like Storm and EF
        var person = await _connection.QuerySingleAsync<Person>(
            "SELECT * FROM Person.Person WHERE BusinessEntityID = @BusinessEntityID",
            new { BusinessEntityID = 1001 });
        person.FirstName = "UpdatedDapper";
        await _connection.ExecuteAsync(
            "UPDATE Person.Person SET FirstName = @FirstName WHERE BusinessEntityID = @BusinessEntityID",
            person);
    }

    [Benchmark]
    public async Task UpdatePersonStorm()
    {
        var person = await _stormContext.SelectFromPerson(1001).GetAsync();
        person!.FirstName = "UpdatedStorm";
        await _stormContext.UpdatePerson().Set(person).GoAsync();
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
    public async Task TrackingAndUpdateEf()
    {
        var person = await _context.Persons.FirstAsync(p => p.BusinessEntityID == 1001);
        person.FirstName = "TrackedEf";
        await _context.SaveChangesAsync();
    }

    [Benchmark]
    public async Task TrackingAndUpdateStorm()
    {
        var person = await _stormContext.SelectFromPerson(1001).WithTracking().GetAsync();
        person!.FirstName = "TrackedStorm";
        await _stormContext.UpdatePerson().Set(person).GoAsync();
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
        // Use all primary key fields: ProductID, Name, ProductNumber, Rowguid
        var product = await _stormContext.SelectFromProduct(1001, "TestProduct", "TP-001", Guid.Empty).GetAsync();
        if (product == null || product.ProductID != 1001)
            throw new Exception("Product not found");
    }

    [Benchmark]
    public async Task UpdateProductEf()
    {
        var product = await _context.Products.FirstAsync(p => p.ProductID == 1001);
        product.Name = "UpdatedEf";
        await _context.SaveChangesAsync();
    }

    [Benchmark]
    public async Task UpdateProductDapper()
    {
        var product = new Product
        {
            ProductID = 1001,
            Name = "UpdatedDapper"
            // Set other required properties if needed
        };
        await _connection.ExecuteAsync(
            "UPDATE Production.Product SET Name = @Name WHERE ProductID = @ProductID",
            product);
    }

    [Benchmark]
    public async Task UpdateProductStorm()
    {
        var product = await _stormContext.SelectFromProduct(1001, "TestProduct", "TP-001", Guid.Empty).GetAsync();
        product!.Name = "UpdatedStorm";
        await _stormContext.UpdateProduct().Set(product).GoAsync();
    }

    [Benchmark]
    public async Task UpdatePersonsEf_1()
    {
        var person = await _context.Persons.FirstAsync(p => p.BusinessEntityID == 1001);
        person.FirstName = "UpdatedEf1";
        await _context.SaveChangesAsync();
    }

    [Benchmark]
    public async Task UpdatePersonsEf_100()
    {
        var persons = await _context.Persons.Where(p => p.BusinessEntityID >= 2201 && p.BusinessEntityID < 2301).ToListAsync();
        foreach (var person in persons)
        {
            person.FirstName = "UpdatedEf100";
        }
        await _context.SaveChangesAsync();
    }

    [Benchmark]
    public async Task UpdatePersonsEf_1000()
    {
        var persons = await _context.Persons.Where(p => p.BusinessEntityID >= 2301 && p.BusinessEntityID < 3301).ToListAsync();
        foreach (var person in persons)
        {
            person.FirstName = "UpdatedEf1000";
        }
        await _context.SaveChangesAsync();
    }

    [Benchmark]
    public async Task UpdatePersonsDapper_1()
    {
        var person = new Person
        {
            BusinessEntityID = 1001,
            FirstName = "UpdatedDapper1"
            // Set other required properties if needed
        };
        await _connection.ExecuteAsync(
            "UPDATE Person.Person SET FirstName = @FirstName WHERE BusinessEntityID = @BusinessEntityID",
            person);
    }

    [Benchmark]
    public async Task UpdatePersonsDapper_100()
    {
        for (var i = 2201; i < 2301; i++)
        {
            var person = new Person
            {
                BusinessEntityID = i,
                FirstName = "UpdatedDapper100"
                // Set other required properties if needed
            };
            await _connection.ExecuteAsync(
                "UPDATE Person.Person SET FirstName = @FirstName WHERE BusinessEntityID = @BusinessEntityID",
                person);
        }
    }

    [Benchmark]
    public async Task UpdatePersonsDapper_1000()
    {
        for (var i = 2301; i < 3301; i++)
        {
            var person = new Person
            {
                BusinessEntityID = i,
                FirstName = "UpdatedDapper1000"
                // Set other required properties if needed
            };
            await _connection.ExecuteAsync(
                "UPDATE Person.Person SET FirstName = @FirstName WHERE BusinessEntityID = @BusinessEntityID",
                person);
        }
    }

    [Benchmark]
    public async Task UpdatePersonsStorm_1()
    {
        var person = await _stormContext.SelectFromPerson(1001).GetAsync();
        person!.FirstName = "UpdatedStorm1";
        await _stormContext.UpdatePerson().Set(person).GoAsync();
    }

    [Benchmark]
    public async Task UpdatePersonsStorm_100()
    {
        var persons = await _stormContext.SelectFromPerson().Top(100).ListAsync();
        foreach (var person in persons)
        {
            person.FirstName = "UpdatedStorm100";
        }
        await _stormContext.UpdatePerson().Set(persons).GoAsync();
    }

    [Benchmark]
    public async Task UpdatePersonsStorm_1000()
    {
        var persons = await _stormContext.SelectFromPerson().Top(1000).ListAsync();
        foreach (var person in persons)
        {
            person.FirstName = "UpdatedStorm1000";
        }
        await _stormContext.UpdatePerson().Set(persons).GoAsync();
    }

    //private static void VerifyHumans(IEnumerable<Human>? humans)
    //{
    //    if (humans is null)
    //    {
    //        Console.WriteLine("VerifyHumans: null");
    //        throw new Exception("null");
    //    }
    //}

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
