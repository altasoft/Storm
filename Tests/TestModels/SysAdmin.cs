using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "SysAdmins", UpdateMode = UpdateMode.ChangeTracking)]
public partial record SysAdmin : User
{
    [StormColumn(ColumnType = ColumnType.ConditionalTerminator)]
    public bool JoinFailed { get; set; }

    public int Sid { get; set; }
}
