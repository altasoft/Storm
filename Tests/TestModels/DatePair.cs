using System;
using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

[StormDbObject<TestStormContext>]
public partial class DatePair
{
    [StormColumn(ColumnName = "DT1", ColumnType = ColumnType.ConcurrencyCheck)]
    public DateOnly Date1 { get; set; }

    [StormColumn(ColumnName = "DT2")]
    public DateOnly? Date2 { get; set; }
}
