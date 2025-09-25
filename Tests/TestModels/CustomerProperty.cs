using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.TestModels.VeryBadNamespace;

namespace AltaSoft.Storm.TestModels;

[StormDbObject<TestStormContext>(SchemaName = "dbo", DisplayName = "CustomerProperties")]
public partial record CustomerProperty
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public CustomerId Id { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50, ColumnType = ColumnType.PrimaryKey)]
    public string Name { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string Value { get; set; }
}
