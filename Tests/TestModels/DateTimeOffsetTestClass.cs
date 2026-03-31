using System;
using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

[StormDbObject<TestStormContext>(SchemaName = "dbo", DisplayName = "DateTimeOffsetTestClass")]
public sealed partial class DateTimeOffsetTestClass
{
    [StormColumn(ColumnType = ColumnType.PrimaryKeyAutoIncrement)]
    public int Id { get; set; }

    [StormColumn(Precision = 7)]
    public DateTimeOffset RequiredOffset { get; set; }

    [StormColumn(Precision = 7)]
    public DateTimeOffset? NullableOffset { get; set; }
}
