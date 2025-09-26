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
public class AdventureWorksUpdateBenchmark : IDisposable, IAsyncDisposable
{
    private readonly AdventureWorksContext _context;
    private readonly AdventureWorksStormContext _stormContext;
    private readonly SqlConnection _connection;

    public AdventureWorksUpdateBenchmark()
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
    public async Task UpdatePersonEf()
    {
        var person = await _context.Persons.FirstAsync(p => p.BusinessEntityID == 1001);
        person.FirstName = "UpdatedEf";
        person.LastName = "LastEf";
        person.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    [Benchmark]
    public async Task UpdatePersonDapper()
    {
        var person = await _connection.QuerySingleAsync<Person>(
            "SELECT * FROM Person.Person WHERE BusinessEntityID = @BusinessEntityID",
            new { BusinessEntityID = 1001 });
        person.FirstName = "UpdatedDapper";
        person.LastName = "LastDapper";
        person.ModifiedDate = DateTime.UtcNow;
        await _connection.ExecuteAsync(
            "UPDATE Person.Person SET FirstName = @FirstName, LastName = @LastName,  ModifiedDate = @ModifiedDate WHERE BusinessEntityID = @BusinessEntityID",
            person);
    }

    [Benchmark]
    public async Task UpdatePersonStorm()
    {
        var person = await _stormContext.SelectFromPerson(1001).GetAsync();
        person!.FirstName = "UpdatedStorm";
        person.LastName = "LastStorm";
        person.ModifiedDate = DateTime.UtcNow;
        await _stormContext.UpdatePerson().Set(person).GoAsync();
    }

    [Benchmark]
    public async Task UpdatePersonsEf_100()
    {
        var persons = await _context.Persons.Where(p => p.BusinessEntityID >= 2201 && p.BusinessEntityID < 2301).ToListAsync();
        foreach (var person in persons)
        {
            person.FirstName = "UpdatedEf100";
            person.LastName = "LastEf100";
            person.ModifiedDate = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }

    [Benchmark]
    public async Task UpdatePersonsDapper_100()
    {
        const int start = 2201;
        const int end = 2300;

        // 1) Read the 100 rows
        var people = (await _connection.QueryAsync<Person>(
                "SELECT * FROM Person.Person WHERE BusinessEntityID BETWEEN @Start AND @End",
                new { Start = start, End = end }))
            .ToList();

        // 2) Modify in memory
        var now = DateTime.UtcNow;
        foreach (var p in people)
        {
            p.FirstName = "UpdatedDapper100";
            p.LastName = "LastDapper100";
            p.ModifiedDate = now;
        }

        // 3) Update all together (multi-exec) inside one transaction
        await using var tx = _connection.BeginTransaction();
        await _connection.ExecuteAsync(
            @"UPDATE Person.Person
          SET FirstName = @FirstName,
              LastName = @LastName,
              ModifiedDate = @ModifiedDate
          WHERE BusinessEntityID = @BusinessEntityID",
            people, // IEnumerable<Person> -> Dapper runs once per item
            transaction: tx);
        tx.Commit();
    }

    [Benchmark]
    public async Task UpdatePersonsStorm_100()
    {
        var persons = await _stormContext.SelectFromPerson().Where(p => p.BusinessEntityID >= 2201 && p.BusinessEntityID < 2301).ListAsync();
        foreach (var person in persons)
        {
            person.FirstName = "UpdatedStorm100";
            person.LastName = "LastStorm100";
            person.ModifiedDate = DateTime.UtcNow;
        }
        await _stormContext.UpdatePerson().Set(persons).GoAsync();
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
