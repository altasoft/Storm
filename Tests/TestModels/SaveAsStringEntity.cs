using System;
using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

/// <summary>
/// Formattable values saved as strings. They must be written and read using the invariant culture.
/// </summary>
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "SaveAsStringEntities", DisplayName = "SaveAsStringEntity", ObjectType = DbObjectType.Table)]
public partial class SaveAsStringEntity
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public int Id { get; set; }

    [StormColumn(SaveAs = SaveAs.String, Size = 50)]
    public decimal Amount { get; set; }

    [StormColumn(SaveAs = SaveAs.String, Size = 50)]
    public decimal? NullableAmount { get; set; }

    [StormColumn(SaveAs = SaveAs.String, Size = 50)]
    public double Ratio { get; set; }

    [StormColumn(SaveAs = SaveAs.String, Size = 50)]
    public DateTime When { get; set; }

    [StormColumn(SaveAs = SaveAs.String, Size = 50)]
    public DateTimeOffset WhenOffset { get; set; }

    [StormColumn(SaveAs = SaveAs.String, Size = 20)]
    public DateOnly Day { get; set; }

    [StormColumn(SaveAs = SaveAs.String, Size = 30)]
    public TimeOnly Time { get; set; }

    [StormColumn(SaveAs = SaveAs.String, Size = 20)]
    public DomainTypes.UserId OwnerId { get; set; }
}
