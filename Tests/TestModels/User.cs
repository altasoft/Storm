using System;
using System.Collections.Generic;
using System.Data.Common;
using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.Interfaces;
using AltaSoft.Storm.TestModels.VeryBadNamespace;

namespace AltaSoft.Storm.TestModels;

/// <summary>
/// See ""
/// </summary>
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "Users", DisplayName = "UsersTable", ObjectType = DbObjectType.Table)]
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "UsersView", DisplayName = "UsersView", ObjectType = DbObjectType.View)]
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "users_func", DisplayName = "UsersFunc", ObjectType = DbObjectType.TableValuedFunction)]
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "users_proc", DisplayName = "UserProc", ObjectType = DbObjectType.StoredProcedure)]
[StormDbObject<TestStormContext>(VirtualViewSql =
    """
    SELECT * FROM {%schema%}.Users
    WHERE Id > 5
    """, DisplayName = "UsersVirtualView", ObjectType = DbObjectType.VirtualView)]
[StormDbObject<TestStormContext>(VirtualViewSql = null,
    DisplayName = "UsersCustomSql", ObjectType = DbObjectType.CustomSqlStatement)]
[StormIndex(["UserId"], false, indexName: "IX_USER_USER_ID")]
[StormIndex(["BranchId"], false, indexName: "IX_USER_BRANCH_ID")]
[StormIndex(["AutoInc"], true, indexName: "IX_USER_AUTOINC")]
public partial record User
{
    private static void UsersFunc(
        [StormParameter(DbType = UnifiedDbType.Int32, ParameterName = "@userId")] DomainTypes.UserId userId)
    {
    }

    private static void UserProc(
        [StormParameter(DbType = UnifiedDbType.Int32, ParameterName = "@userId")] DomainTypes.UserId userId,
        ref CustomerId? io
        )
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

    [StormColumn(SaveAs = SaveAs.Ignore)]
    public string? IgnoredValue { get; set; }

    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
    public DomainTypes.UserId UserId { get; set; }

    [StormColumn(ColumnType = ColumnType.PrimaryKey, DbType = UnifiedDbType.Int16)]
    public short BranchId { get; set; }

    [StormColumn(ColumnType = ColumnType.AutoIncrement)]
    public int AutoInc { get; set; }

    [DbProviderSpecificTypeProperty(true)]
    [StormColumn(ColumnType = ColumnType.RowVersion | ColumnType.ConcurrencyCheck)]
    public SqlRowVersion RowVersion { get; set; }

    [DbProviderSpecificTypeProperty(true)]
    [StormColumn]
    public SqlLogSequenceNumber Lsn { get; set; }

    [StormColumn(ColumnType = ColumnType.ConcurrencyCheck)]
    public int Version { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 20)]
    public string LoginName { get; set; }

    [StormColumn(LoadWithFlags = true, Size = 100)]
    public string? FullName { get; set; }

    //[StormColumn(SaveAs = SaveAs.Xml, DbType = UnifiedDbType.AnsiString, Size = 1000)]
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

    public UserStatus UserStatus { get; set; }
    public UserStatus? NullableUserStatus { get; set; }
    public TrackableObject? TrackableObject { get; set; }

    [StormColumn(SaveAs = SaveAs.CompressedString, Size = 1000)]
    public string? BigString { get; set; }
}

public enum UserStatus
{
    Ok,
    Pending,
    Blocked
}
