using System;
using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

/// <summary>
/// Entity with TimeSpan properties stored in SQL <c>time</c> columns.
/// The generated reader must read them as TimeSpan (not via GetTime, which returns TimeOnly).
/// </summary>
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "TimeSpanEntities", DisplayName = "TimeSpanEntity", ObjectType = DbObjectType.Table)]
public partial class TimeSpanEntity
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public int Id { get; set; }

    public TimeSpan Duration { get; set; }

    public TimeSpan? OptionalDuration { get; set; }
}
