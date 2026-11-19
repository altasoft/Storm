using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

/// <summary>
/// Entity with a <c>decimal</c> identity primary key (no explicit precision, so the column is created as <c>decimal(19,0)</c>).
/// </summary>
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "DecimalIdentityEntities", DisplayName = "DecimalIdentityEntity", ObjectType = DbObjectType.Table)]
public partial class DecimalIdentityEntity
{
    [StormColumn(ColumnType = ColumnType.PrimaryKeyAutoIncrement)]
    public decimal Id { get; set; }

    [StormColumn(Size = 50)]
    public string Name { get; set; } = "";
}
