using AltaSoft.Storm.Attributes;

namespace TestProj;

[StormDbObject(UpdateMode = UpdateMode.UpdateAll)]
public partial class DatePair
{
    [StormColumn(ColumnName = "DT1")]
    public DateTime Date1 { get; set; }

    [StormColumn(ColumnName = "DT2")]
    public DateTime? Date2 { get; set; }

    //public (StormColumnDef column, object? value)[] __ColumnValues2()
    //{
    //    var ctrl = StormControllerCache.Get(typeof(DatePair));
    //    var columnDefs = DatePairStormController.__columnDefs;

    //    return [
    //        (columnDefs[0], Date1),
    //        (columnDefs[1], Date2)
    //    ];
    //}
}
