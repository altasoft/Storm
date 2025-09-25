using AltaSoft.Storm;
using AltaSoft.Storm.Attributes;

namespace TestProj;

[StormDbObject(SchemaName = "dbo", ObjectName = "Users", UpdateMode = UpdateMode.ChangeTracking)]
public abstract partial record BaseClass
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
    public int UserId { get; set; }

    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
    public int? UserIdN { get; set; }

    [StormColumn(SaveAs = SaveAs.DetailTable, DbType = UnifiedDbType.AnsiString, Size = 20)]
    public TrackingList<string>? Lll { get; set; }

    [StormColumn(SaveAs = SaveAs.Xml)]
    public List<int>? Roles { get; set; }

    [StormColumn(SaveAs = SaveAs.FlatObject, ColumnName = "DP")]
    public DatePair DatePair { get; set; }

    //public int Id { get; set; }
    //public int? IdN { get; set; }
    //public string? Ids { get; set; }

    //private string _test;

    //[StormColumn(SaveAs = SaveAs.Ignore)]
    //public string Test
    //{
    //    get => _test;
    //    set
    //    {
    //        var __oldValue = _test;

    //        _test = value;

    //        __PropertySet_Test(value, __oldValue);
    //    }
    //}
}
