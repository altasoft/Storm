//#nullable enable

//using BenchmarkDotNet.Attributes;
//using Microsoft.Data.SqlClient;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.CompilerServices;
//using System.Threading.Tasks;
//using AltaSoft.Storm.TestModels;
//using Dapper;
//using Microsoft.EntityFrameworkCore;

//namespace AltaSoft.Storm.Benchmarks;

//public class BenchContext : DbContext
//{
//    public static readonly SqlConnection Connection = GetSqlConnection();

//    public BenchContext() : base(GetOptions())
//    {
//        // No-op.
//    }

//    public DbSet<Human> Humans => Set<Human>();

//    protected override void OnModelCreating(ModelBuilder modelBuilder)
//    {
//        modelBuilder.Entity<Human>(entity =>
//        {
//            // Set the 'XId' as primary key
//            entity.HasKey(e => e.XId);

//            // Configure 'XId' as a non-identity column
//            entity.Property(e => e.XId)
//                .ValueGeneratedNever(); // This is important

//            // Optionally, specify the column type if needed
//            entity.Property(e => e.XId)
//                .HasColumnType("bigint");
//        });
//    }

//    private static DbContextOptions<BenchContext> GetOptions()
//    {
//        return new DbContextOptionsBuilder<BenchContext>()
//            .UseSqlServer(Connection)
//            .Options;
//    }

//    private static SqlConnection GetSqlConnection()
//    {
//        var connection = new SqlConnection(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Benchmark;Integrated Security=True;Encrypt=false;TrustServerCertificate=true");
//        connection.Open();
//        return connection;
//    }
//}

//[MemoryDiagnoser]
//public class EfCoreVsDapperBench : IDisposable, IAsyncDisposable
//{
//    //private static readonly Func<BenchContext, Human> CompiledHumanQuery =
//    //    EF.CompileQuery((BenchContext ctx) => ctx.Humans.AsNoTracking().SingleAsync(p => p.XId == 1));

//    //private static readonly Func<BenchContext, long, string> CompiledNameQuery =
//    //    EF.CompileQuery((BenchContext ctx, long id) =>
//    //        ctx.Humans.Where(p => p.XId == id).Select(p => p.Name).Single());

//    private readonly BenchContext _context;

//    private readonly DefaultStormContext _stormContext;

//    private readonly SqlConnection _connection;

//    public EfCoreVsDapperBench()
//    {
//        _stormContext = new DefaultStormContext(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Benchmark;Integrated Security=True;Encrypt=false;TrustServerCertificate=true");

//        _context = new BenchContext();
//        _context.Database.EnsureCreated();
//        _context.Humans.ExecuteDelete();
//        for (var i = 0; i < 1000; i++)
//        {
//            _context.Humans.Add(new Human { XId = i, Name = "Human" + i, Age = long.MaxValue - i, Amount = i, Ccy = "USD" });
//        }
//        _context.SaveChanges();

//        StormManager.Initialize(new MsSqlOrmProvider(), "dbo");

//        _connection = new SqlConnection(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Benchmark;Integrated Security=True;Encrypt=false;TrustServerCertificate=true");
//        _connection.Open();
//    }

//    [Benchmark]
//    public async Task SelectEntityByIdEf()
//    {
//        var human = await _context.Humans.AsNoTracking().FirstAsync(p => p.XId == 1);
//        VerifyHuman(human);
//    }

//    [Benchmark]
//    public async Task SelectEntityByIdDapper()
//    {
//        var human = await BenchContext.Connection.QuerySingleAsync<Human>(
//            "SELECT * FROM Humans WHERE XId = @id", new { id = 1 });
//        VerifyHuman(human);
//    }

//    [Benchmark]
//    public async Task SelectEntityByIdStorm()
//    {
//        var human = await _stormContext.SelectFromHuman(1).WithNoTracking().GetAsync();
//        VerifyHuman(human!);
//    }

//    //[Benchmark]
//    //public async Task SelectEntityByIdStorm2()
//    //{
//    //    var human = await _connection.SelectFromHuman(1).GetAsync();
//    //    VerifyHuman(human!);
//    //}

//    [Benchmark]
//    public async Task SelectFieldByIdEf()
//    {
//        var name = await _context.Humans.Where(p => p.XId == 2).Select(p => p.Name).FirstAsync();
//        VerifyName(name);
//    }

//    [Benchmark]
//    public async Task SelectFieldByIdDapper()
//    {
//        var name = await BenchContext.Connection.QuerySingleAsync<string>("SELECT name FROM Humans WHERE XId = @id", new { id = 2 });
//        VerifyName(name);
//    }

//    [Benchmark]
//    public async Task SelectFieldByIdStorm()
//    {
//        var name = await _stormContext.SelectFromHuman(2).GetAsync(x => x.Name);
//        VerifyName(name);
//    }

//    //[Benchmark]
//    //public async Task SelectFieldByIdStorm2()
//    //{
//    //    var name = await _connection.SelectFromHuman(2).GetAsync(x => x.Name);
//    //    VerifyName(name);
//    //}

//    [Benchmark]
//    public async Task SelectEntitiesEf()
//    {
//        var humans = await _context.Humans.AsNoTracking().ToListAsync();
//        VerifyHumans(humans);
//    }

//    //[Benchmark]
//    //public void SelectEntitiesByIdEfCompiled()
//    //{
//    //    var human = CompiledHumanQuery(_context);
//    //    VerifyHuman(human);
//    //}

//    [Benchmark]
//    public async Task SelectEntitiesDapper()
//    {
//        var humans = await BenchContext.Connection.QueryAsync<Human>("SELECT * FROM Humans");
//        VerifyHumans(humans);
//    }

//    [Benchmark]
//    public async Task SelectEntitiesStorm()
//    {
//        var humans = await _stormContext.SelectFromHuman().ListAsync();
//        VerifyHumans(humans);
//    }

//    //[Benchmark]
//    //public async Task SelectEntitiesStorm2()
//    //{
//    //    var humans = await _connection.SelectFromHuman().ListAsync();
//    //    VerifyHumans(humans);
//    //}

//    [Benchmark]
//    public async Task UpdateEntitiesEf()
//    {
//        var humans = await _context.Humans.Take(10).ToArrayAsync();
//        foreach (var human in humans)
//        {
//            human.Name = human.Name.Reverse().ToString()!;
//        }
//        await _context.SaveChangesAsync();
//    }

//    [Benchmark]
//    public async Task UpdateEntitiesStorm()
//    {
//        var humans = await _stormContext.SelectFromHuman().Top(10).WithTracking().ListAsync();
//        foreach (var human in humans)
//        {
//            human.Name = human.Name.Reverse().ToString()!;
//        }
//        await _stormContext.UpdateHuman().Set(humans).GoAsync();
//    }

//    //[Benchmark]
//    //public async Task UpdateEntitiesStorm2()
//    //{
//    //    var humans = await _connection.SelectFromHuman().Top(10).WithTracking().ListAsync();
//    //    foreach (var human in humans)
//    //    {
//    //        human.Name = human.Name.Reverse().ToString()!;
//    //    }
//    //    await _connection.UpdateHuman().Set(humans).GoAsync();
//    //}

//    private static void VerifyHumans(IEnumerable<Human>? humans)
//    {
//        if (humans is null)
//        {
//            Console.WriteLine("VerifyHumans: null");
//            throw new Exception("null");
//        }
//    }

//    private static void VerifyHuman(Human human)
//    {
//        if (human.XId != 1)
//        {
//            Console.WriteLine($"VerifyHuman: Human.XId = {human.XId}");
//            throw new Exception(human.Name);
//        }
//    }

//    private static void VerifyName(string name)
//    {
//        if (name != "Human2")
//        {
//            Console.WriteLine($"VerifyName: name = {name}");
//            throw new Exception(name);
//        }
//    }

//    public void Dispose()
//    {
//        _context.Dispose();
//        _stormContext.Dispose();
//        _connection.Dispose();
//    }

//    public async ValueTask DisposeAsync()
//    {
//        await _context.DisposeAsync();
//        await _stormContext.DisposeAsync();
//        await _connection.DisposeAsync();
//    }
//}
