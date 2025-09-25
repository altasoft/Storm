using BenchmarkDotNet.Running;

namespace AltaSoft.Storm.Benchmarks;

internal sealed class Program
{
    private static void Main(string[] args)
    {
        // Bench
        BenchmarkRunner.Run<AdventureWorksBenchmark>();
    }
}

//public static class Program
//{
//    public static async Task Main(string[] args)
//    {
//        var x = new Post();
//#if DEBUG
//        WriteLineColor("Warning: DEBUG configuration; performance may be impacted!", ConsoleColor.Red);
//        WriteLine();
//#endif
//        WriteLine("Welcome to Dapper's ORM performance benchmark suite, based on BenchmarkDotNet.");
//        Write("  If you find a problem, please report it at: ");
//        WriteLineColor("https://github.com/DapperLib/Dapper", ConsoleColor.Blue);
//        WriteLine("  Or if you're up to it, please submit a pull request! We welcome new additions.");
//        WriteLine();

//        if (args.Length == 0)
//        {
//            WriteLine("Optional arguments:");
//            WriteColor("  (no args)", ConsoleColor.Blue);
//            WriteLine(": run all benchmarks");
//            WriteColor("  --legacy", ConsoleColor.Blue);
//            WriteLineColor(": run the legacy benchmark suite/format", ConsoleColor.Gray);
//            WriteLine();
//        }
//        WriteLine("Using ConnectionString: " + BenchmarkBase.ConnectionString);

//        await EnsureDbSetupAsync(CancellationToken.None);

//        WriteLine("Database setup complete.");

//        WriteLine("Iterations: " + Config.Iterations);

//        new BenchmarkSwitcher(typeof(BenchmarkBase).Assembly).Run(args, new Config());
//    }

//    private static async Task EnsureDbSetupAsync(CancellationToken cancellationToken)
//    {
//        await using var conn = new Microsoft.Data.SqlClient.SqlConnection(BenchmarkBase.ConnectionString);

//        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

//        var cmd = conn.CreateCommand();

//        await conn.CreateTableAsync<Post>(true, cancellationToken: cancellationToken);

//        await conn.DropTableAsync<Post>(true, cancellationToken: cancellationToken);

//        for (var i = 0; i < 500; i++)
//        {
//            await conn.InsertAsync(new Post
//            {
//                Id = i,
//                Text = new string('x', 2000),
//                CreationDate = DateTime.Now,
//                LastChangeDate = DateTime.Now
//            }, cancellationToken: cancellationToken);
//        }
//    }

//    public static void WriteLineColor(string message, ConsoleColor color)
//    {
//        var orig = ForegroundColor;
//        ForegroundColor = color;
//        WriteLine(message);
//        ForegroundColor = orig;
//    }

//    public static void WriteColor(string message, ConsoleColor color)
//    {
//        var orig = ForegroundColor;
//        ForegroundColor = color;
//        Write(message);
//        ForegroundColor = orig;
//    }
//}
