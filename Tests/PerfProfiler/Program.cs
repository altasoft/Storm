using AltaSoft.Storm;
using AltaSoft.Storm.TestModels;
using AltaSoft.Storm.Tests;
using Microsoft.Data.SqlClient;

internal sealed class Program : IAsyncDisposable
{
    private const string TestBaseConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True;Encrypt=false;TrustServerCertificate=true";

    private static DatabaseHelper s_databaseHelper = default!;

    public static string ConnectionString => s_databaseHelper.ConnectionString;

    //public static IEnumerable<ConstructorInfo> GetAllStaticConstructorsInSolution()
    //{
    //    var assemblies = AppDomain.CurrentDomain.GetAssemblies();

    //    return assemblies.SelectMany(assembly => assembly.DefinedTypes
    //            .Where(type => type.DeclaredConstructors.Any(constructorInfo => constructorInfo.IsStatic))
    //            .SelectMany(x => x.GetConstructors(BindingFlags.Static)))
    //        .Distinct();
    //}

    internal static async Task Main(string[] args)
    {
        StormManager.Initialize(new MsSqlOrmProvider(), configuration =>
        {
            configuration.AddStormContext<TestStormContext>(dbConfig =>
            {
                dbConfig.UseConnectionString(ConnectionString);
                dbConfig.UseDefaultSchema("dbo");
            });
        });

        //foreach (var constructorInfo in GetAllStaticConstructorsInSolution())
        //{
        //    Console.WriteLine(constructorInfo.Name);
        //}

        s_databaseHelper = new DatabaseHelper("storm-perf-test-" + Guid.NewGuid(), TestBaseConnectionString);

        await s_databaseHelper.InitializeAsync();

        //var logger = new XunitLogger<DatabaseFixture>(output);
        //StormManager.SetLogger(logger);

        await using var connection = new SqlConnection(ConnectionString);

        await connection.CreateTableAsync<Human>(true).ConfigureAwait(false);

        var humans = new List<Human>(10);
        for (var i = 1; i <= 10; i++)
        {
            humans.Add(new Human { XId = i, Name = "1", Age = 2, Amount = 3, Ccy = "USD" });
        }

        await using var context = new TestStormContext();

        await context.InsertIntoHuman().Values(humans).GoAsync();
        await TestHumanAsync(context);
    }

    private static async Task TestHumanAsync(TestStormContext context)
    {
        //var x = StormControllerCache.Get<Human>().QuerySingleScalarAsync<Human, long>(connection, "", x => x.XId);
        //StormCrudFactory.SelectFrom<Human, Human.OrderBy, Human.PartialLoadFlags>(context, 2);
        var _ = await context.SelectFromHuman(2).GetAsync();

        try
        {
            Console.WriteLine();
            var x = await context.SelectFromHuman(2).GetAsync();
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        //var x = await context.SelectFromHuman().Where(x => x.XId > 2).ListAsync(x => x.XId);

        //_ = await connection.GetHumanByKeyAsync(1);

        //var y = await context.SelectFromHuman()
        //    .OrderBy(Human.OrderByKey)
        //    .ListAsync();

        //var z = await context.SelectFromHuman()
        //    .OrderBy(Human.OrderBy.Name, Human.OrderBy.XId_Desc)
        //    .GetAsync(x => x.XId, x => x.Name);

        //await context.UpdateHuman().Set(new Human()).GoAsync();

        //await context.UpdateHuman()
        //    .Set(x => x.XId, 2)
        //    .Set(x => x.Name, "aaa")
        //    .Where(x => x.XId > 4)
        //    .GoAsync();
        //await connection.DeleteFromUser().GoAsync();
    }

    public async ValueTask DisposeAsync()
    {
        //StormManager.SetLogger(null);

        await s_databaseHelper.DisposeAsync();
    }
}
