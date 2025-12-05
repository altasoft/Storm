using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;


[StormDbObject<TestStormContext>(ObjectType = DbObjectType.CustomSqlStatement, UpdateMode = UpdateMode.ChangeTracking, BulkInsert = true)]
public partial record Blob
{
    [StormColumn(ColumnType = ColumnType.PrimaryKeyAutoIncrement)]
    public int Id { get; set; }

    [StormColumn(SaveAs = SaveAs.CompressedString, Size = 1000)]
    public string? BigString { get; set; }

    [StormColumn(SaveAs = SaveAs.CompressedString, Size = 1000)]
    public required string Metadata { get; set; }

    public long SomeOtherValue { get; set; }
}

