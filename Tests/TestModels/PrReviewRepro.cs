using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

// Models used by PrReviewReproTests to reproduce issues found during the review of the bugfix/somebugs branch.

/// <summary>
/// Flat type with a read-only (computed) sub-property before a regular one.
/// </summary>
[StormDbObject<TestStormContext>]
public partial class FlatWithReadOnly
{
    public int A { get; set; }

    public int Computed => A * 2;

    public int B { get; set; }
}

/// <summary>
/// Entity whose FlatObject contains a read-only sub-property, followed by a regular column.
/// </summary>
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "ReadOnlyFlatEntities", DisplayName = "ReadOnlyFlatEntity", ObjectType = DbObjectType.Table)]
public partial class ReadOnlyFlatEntity
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public int Id { get; set; }

    [StormColumn(SaveAs = SaveAs.FlatObject, ColumnName = "Flat")]
    public FlatWithReadOnly Flat { get; set; } = new();

    public int Last { get; set; }
}
