//using AltaSoft.StormManager;
//using BenchmarkDotNet.Attributes;
//using System.ComponentModel;
//using System.Threading.Tasks;
//using AltaSoft.StormManager.TestModels;

//namespace AltaSoft.Orm.Benchmarks
//{
//    [Description("AltaSoft.Orm")]
//    public class DapperBenchmarks : BenchmarkBase
//    {
//        [GlobalSetup]
//        public void Setup()
//        {
//            BaseSetup();
//            StormManager.Common.Orm.Initialize(new MsSqlOrmProvider(), "dbo", new JsonSerializationProvider(), null);
//        }

//        [Benchmark(Description = "Post.GetByKeyAsync")]
//        public Task<Post> QueryBuffered()
//        {
//            Step();
//            return Post.GetByKeyAsync(Connection, I);
//        }
//    }
//}
