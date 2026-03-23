using System.Collections.Generic;
using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

/// <summary>
/// A test entity with no primary key columns, used to verify that
/// CreateTableAsync omits the PK constraint and skips detail tables.
/// </summary>
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "NoPkEntity", DisplayName = "NoPkEntity", ObjectType = DbObjectType.Table)]
public partial record NoPrimaryKeyEntity
{
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string Name { get; set; } = string.Empty;

    public int Value { get; set; }

    [StormColumn(SaveAs = SaveAs.DetailTable, DbType = UnifiedDbType.AnsiString, ColumnName = "Tag", Size = 50, DetailTableName = "NoPkEntityTags")]
    public List<string>? Tags { get; set; }
}
