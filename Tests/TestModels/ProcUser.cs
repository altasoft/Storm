using System;
using System.Collections.Generic;
using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.Interfaces;
using AltaSoft.Storm.TestModels.VeryBadNamespace;

namespace AltaSoft.Storm.TestModels;

/// <summary>
/// See ""
/// </summary>
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "MyProc", UpdateMode = UpdateMode.NoUpdates, ObjectType = DbObjectType.StoredProcedure)]
internal sealed partial record ProcUser
{
    // Template method
    private static void MyProc()
    {
    }

    public void AfterLoad(uint partialFlags)
    {
        Console.WriteLine($"After load. Flags = {partialFlags}");
    }

    public void BeforeSave(SaveAction action)
    {
        //Console.WriteLine($"Before save. Action = {action}");
    }

    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
    public DomainTypes.UserId UserId { get; set; }

    [StormColumn(ColumnType = ColumnType.PrimaryKey, DbType = UnifiedDbType.Int16)]
    public short BranchId { get; set; }

    [StormColumn(ColumnType = ColumnType.AutoIncrement)]
    public int AutoInc { get; set; }

    [StormColumn(ColumnType = ColumnType.RowVersion | ColumnType.ConcurrencyCheck)]
    public SqlRowVersion RowVersion { get; set; }

    [StormColumn(ColumnType = ColumnType.ConcurrencyCheck)]
    public int Version { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 20)]
    public string LoginName { get; set; }

    [StormColumn(LoadWithFlags = true, Size = 100)]
    public string? FullName { get; set; }

    [StormColumn(SaveAs = SaveAs.Json)]
    public List<int>? Roles { get; set; }

    [StormColumn(SaveAs = SaveAs.FlatObject, ColumnName = "DP")]
    public DatePair DatePair { get; set; }

    public CustomerId CustomerId { get; set; }
    public CustomerId? CustomerId2 { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiStringFixedLength, Size = 3)]
    public CurrencyId CurrencyId { get; set; }

    [StormColumn(SaveAs = SaveAs.Json, DbType = UnifiedDbType.Json, Size = 101)]
    public TwoValues? TwoValues { get; set; }

    [StormColumn(SaveAs = SaveAs.DetailTable, DetailTableName = "UserDates")]
    public List<DatePair>? Dates { get; set; }

    [StormColumn(SaveAs = SaveAs.DetailTable)]
    public EntityTrackingList<Car>? Cars { get; set; }

    [StormColumn(SaveAs = SaveAs.DetailTable, DbType = UnifiedDbType.AnsiString, ColumnName = "StringValue", Size = 100)]
    public TrackingList<string>? ListOfStrings { get; set; }

    [StormColumn(SaveAs = SaveAs.DetailTable, DbType = UnifiedDbType.Int32, ColumnName = "IntegerValue")]
    public TrackingList<int>? ListOfIntegers { get; set; }
}
