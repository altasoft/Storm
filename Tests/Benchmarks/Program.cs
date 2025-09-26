using BenchmarkDotNet.Running;

namespace AltaSoft.Storm.Benchmarks;

internal sealed class Program
{
    private static void Main(string[] args)
    {
        // Bench
        BenchmarkRunner.Run<AdventureWorksUpdateBenchmark>();
    }
}
