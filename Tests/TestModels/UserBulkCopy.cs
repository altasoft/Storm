using System;
using System.Collections.Generic;
using System.Data.Common;
using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.TestModels.VeryBadNamespace;

namespace AltaSoft.Storm.TestModels;

/// <summary>
/// See ""
/// </summary>
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "UsersBulkCopy", DisplayName = "UsersBulkCopy", ObjectType = DbObjectType.Table, BulkInsert = true)]

public partial record UserBulkCopy
{

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

    public UserStatus UserStatus { get; set; }
    public UserStatus? NullableUserStatus { get; set; }
    public TrackableObject? TrackableObject { get; set; }

    [StormColumn(SaveAs = SaveAs.CompressedString, Size = 1000)]
    public string? BigString { get; set; }
}
