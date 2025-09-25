namespace AltaSoft.Storm.TestModels;

[AltaSoft.Storm.Attributes.StormDbObject<TestStormContext>]
public partial class ClassWithTimestamp
{
    [AltaSoft.Storm.Attributes.StormColumn(ColumnType = AltaSoft.Storm.Attributes.ColumnType.PrimaryKey | AltaSoft.Storm.Attributes.ColumnType.RowVersion | AltaSoft.Storm.Attributes.ColumnType.ConcurrencyCheck)]
    public SqlRowVersion EventId { get; set; }

    public string Name { get; set; }
}
