using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels
{
    [StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "SysAdminsInherited")]
    public partial record SysAdminInherited : SysAdmin
    {
        [StormColumn(ColumnType = ColumnType.ConditionalTerminator)]
        public bool JoinFailed2 { get; set; }

        public int Sid2 { get; set; }
    }
}
