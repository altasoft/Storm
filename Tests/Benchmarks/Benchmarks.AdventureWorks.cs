#nullable enable

using System;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels.AdventureWorks;
using BenchmarkDotNet.Attributes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AltaSoft.Storm.Benchmarks;

public class AdventureWorksContext : DbContext
{
    public static readonly SqlConnection Connection = GetSqlConnection();

    public AdventureWorksContext() : base(GetOptions())
    {
        // No-op.
    }

    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(e => e.BusinessEntityID);

            // Configure 'XId' as a non-identity column
            entity.Property(e => e.BusinessEntityID)
                .ValueGeneratedNever(); // This is important
        });
    }

    private static DbContextOptions<AdventureWorksContext> GetOptions()
    {
        return new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseSqlServer(Connection)
            .Options;
    }

    private static SqlConnection GetSqlConnection()
    {
        var connection = new SqlConnection("Data Source=docker2;Initial Catalog=AdventureWorks;Persist Security Info=True;User ID=sa;Password=Pa$$w0rd;Trust Server Certificate=True");
        connection.Open();
        return connection;
    }
}

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
        _stormContext = new AdventureWorksStormContext("Data Source=docker2;Initial Catalog=AdventureWorks;Persist Security Info=True;User ID=sa;Password=Pa$$w0rd;Trust Server Certificate=True");

        if (!StormManager.IsInitialized)
            StormManager.Initialize(new MsSqlOrmProvider(), "dbo");

        // For dapper
        _connection = new SqlConnection("Data Source=docker2;Initial Catalog=AdventureWorks;Persist Security Info=True;User ID=sa;Password=Pa$$w0rd;Trust Server Certificate=True");
        _connection.Open();
    }

    [Benchmark]
    public async Task SelectPersonByIdEf()
    {
        var person = await _context.Persons.AsNoTracking().FirstAsync(p => p.BusinessEntityID == 1001);
        VerifyPerson(person);
    }

    [Benchmark]
    public async Task SelectPersonByIdDapper()
    {
        var person = await _connection.QuerySingleAsync<Person>(
            """
            SELECT [BusinessEntityID]
                ,[PersonType]
                ,[NameStyle]
                ,[Title]
                ,[FirstName]
                ,[MiddleName]
                ,[LastName]
                ,[Suffix]
                ,[EmailPromotion]
                ,[AdditionalContactInfo]
                ,[Demographics]
                ,[rowguid]
                ,[ModifiedDate]
            FROM Person.Person
            WHERE BusinessEntityID = @id
            """, new { id = 1001 });

        VerifyPerson(person);
    }

    [Benchmark]
    public async Task SelectPersonByIdStorm()
    {
        var person = await _stormContext.SelectFromPerson(1001).GetAsync();
        VerifyPerson(person!);
    }

    //private static void VerifyHumans(IEnumerable<Human>? humans)
    //{
    //    if (humans is null)
    //    {
    //        Console.WriteLine("VerifyHumans: null");
    //        throw new Exception("null");
    //    }
    //}

    private static void VerifyPerson(Person person)
    {
        if (person.BusinessEntityID != 1001)
        {
            Console.WriteLine($"VerifyPerson: Person.BusinessEntityID = {person.BusinessEntityID}");
            throw new Exception(person.FirstName);
        }
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
