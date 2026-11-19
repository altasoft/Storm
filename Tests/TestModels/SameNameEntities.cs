using AltaSoft.Storm.Attributes;

// Two [StormDbObject] types with the same name in different namespaces.
// The generator must produce distinct source hint names for them (otherwise CS8785 kills all generator output).

namespace AltaSoft.Storm.TestModels.SameNameA
{
    [StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "SameNameEntitiesA", DisplayName = "SameNameEntityA", ObjectType = DbObjectType.Table)]
    public partial class SameNameEntity
    {
        [StormColumn(ColumnType = ColumnType.PrimaryKey)]
        public int Id { get; set; }

        [StormColumn(Size = 50)]
        public string? Name { get; set; }
    }
}

namespace AltaSoft.Storm.TestModels.SameNameB
{
    [StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "SameNameEntitiesB", DisplayName = "SameNameEntityB", ObjectType = DbObjectType.Table)]
    public partial class SameNameEntity
    {
        [StormColumn(ColumnType = ColumnType.PrimaryKey)]
        public int Id { get; set; }

        public int Value { get; set; }
    }
}
