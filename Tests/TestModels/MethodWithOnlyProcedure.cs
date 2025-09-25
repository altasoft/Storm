using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "AddDocument", ObjectType = DbObjectType.StoredProcedure)]
public partial class MethodWithOnlyProcedure
{
    private static void AddDocument(
        [StormParameter(DbType = UnifiedDbType.Int32, ParameterName = "@rec_id")]
        out long recId,
        [StormParameter(DbType = UnifiedDbType.Int32, ParameterName = "@user_id")]
        int userId,
        [StormParameter(DbType = UnifiedDbType.Int32, ParameterName = "@debit_id")]
        int debitId,
        [StormParameter(DbType = UnifiedDbType.Int32, ParameterName = "@credit_id")]
        int creditId,
        [StormParameter(DbType = UnifiedDbType.Int32, ParameterName = "@tariff_id")]
        int? tariffId,
        [StormParameter(DbType = UnifiedDbType.Decimal, ParameterName = "@amount")]
        decimal amount,
        [StormParameter(DbType = UnifiedDbType.Decimal, ParameterName = "@tariff_amount")]
        decimal tariffAmount,
        [StormParameter(DbType = UnifiedDbType.Int32, ParameterName = "@channel_id")]
        int channelId,
        [StormParameter(DbType = UnifiedDbType.AnsiString, Size = 150, ParameterName = "@descrip")]
        string description,
        [StormParameter(DbType = UnifiedDbType.AnsiString, Size = 250, ParameterName = "@extra_descrip")]
        string extraDescription)

    {
        recId = 1;
    }
}
