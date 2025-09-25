using System;
using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.TestModels.VeryBadNamespace;

namespace AltaSoft.Storm.TestModels;

public sealed partial class TestStormContext : StormContext
{
    public TestStormContext(string connectionString) : base(connectionString)
    {
    }

    public TestStormContext() : base()
    {
    }
    
    [StormProcedure(ObjectName = "InputOutputProc", SchemaName = "test")]
    private static int InputOutputProc(
        [StormParameter(ParameterName = "@user_id", DbType = UnifiedDbType.Int32)]
        DomainTypes.UserId? userId,
        [StormParameter(ParameterName = "@result_id", DbType = UnifiedDbType.Int32)]
        out int resultValue,
        [StormParameter(ParameterName = "@io", DbType = UnifiedDbType.Int32)]
        ref int io
    ) => throw new NotImplementedException();

    [StormFunction(ObjectName = "ScalarFunc2", DbType = UnifiedDbType.Int32)]
    private static CustomerId ScalarFunc(
        [StormParameter(ParameterName = "@user_id", DbType = UnifiedDbType.Int32)]
        DomainTypes.UserId userId,
        [StormParameter(ParameterName = "@branch_id", DbType = UnifiedDbType.Int16)]
        int branchId
    ) => default;

    [StormFunction(ObjectName = "ScalarFuncXXX", DbType = UnifiedDbType.Int32)]
    private static CustomerId ScalarFunc3() => default;
}
