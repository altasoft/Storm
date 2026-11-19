using System.Collections.Generic;
using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

// Models with FlatObject properties. They verify that column ids in the generated code account for the fact that
// a FlatObject property occupies one column per sub-property.

/// <summary>
/// Flat type with two columns.
/// </summary>
[StormDbObject<TestStormContext>]
public partial class FlatPair
{
    public int A { get; set; }

    [StormColumn(Size = 50)]
    public string? B { get; set; }
}

/// <summary>
/// AutoIncrement column declared after a FlatObject property (the __columnDefs index of Seq is 3, not 2).
/// </summary>
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "AutoIncFlatEntities", DisplayName = "AutoIncFlatEntity", ObjectType = DbObjectType.Table)]
public partial class AutoIncFlatEntity
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public int Id { get; set; }

    [StormColumn(SaveAs = SaveAs.FlatObject, ColumnName = "Flat")]
    public FlatPair Flat { get; set; } = new();

    [StormColumn(ColumnType = ColumnType.AutoIncrement)]
    public int Seq { get; set; }
}

/// <summary>
/// Base type with a ConditionalTerminator followed by a FlatObject property.
/// When <see cref="Stop"/> is true, the reader must skip the 2 columns of <see cref="Flat"/>.
/// </summary>
[StormDbObject<TestStormContext>]
public partial class TerminatorFlatBase
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public int Id { get; set; }

    [StormColumn(ColumnType = ColumnType.ConditionalTerminator)]
    public bool Stop { get; set; }

    [StormColumn(SaveAs = SaveAs.FlatObject, ColumnName = "Flat")]
    public FlatPair Flat { get; set; } = new();
}

/// <summary>
/// Derived type whose own columns are read after the base type's columns.
/// </summary>
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "TerminatorFlatEntities", DisplayName = "TerminatorFlatEntity", ObjectType = DbObjectType.Table)]
public partial class TerminatorFlatDerived : TerminatorFlatBase
{
    public int After { get; set; }
}

/// <summary>
/// Flat type with a partially loaded (nullable Json) sub-property.
/// </summary>
[StormDbObject<TestStormContext>]
public partial class FlatWithJson
{
    public int Code { get; set; }

    [StormColumn(SaveAs = SaveAs.Json, Size = 200)]
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Parent with its own partial load flag (Notes) and a FlatObject with a partial load flag (Flat.Tags).
/// The flags must not collide.
/// </summary>
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "PartialFlatEntities", DisplayName = "PartialFlatEntity", ObjectType = DbObjectType.Table)]
public partial class PartialFlatEntity
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public int Id { get; set; }

    [StormColumn(LoadWithFlags = true, Size = 100)]
    public string? Notes { get; set; }

    [StormColumn(SaveAs = SaveAs.FlatObject, ColumnName = "Flat")]
    public FlatWithJson Flat { get; set; } = new();
}

/// <summary>
/// Parent without its own partial load flags; only the FlatObject sub-property is partially loaded.
/// </summary>
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "PartialFlatOnlyEntities", DisplayName = "PartialFlatOnlyEntity", ObjectType = DbObjectType.Table)]
public partial class PartialFlatOnlyEntity
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public int Id { get; set; }

    [StormColumn(SaveAs = SaveAs.FlatObject, ColumnName = "Flat")]
    public FlatWithJson Flat { get; set; } = new();
}

/// <summary>
/// Flat type containing a concurrency check column.
/// </summary>
[StormDbObject<TestStormContext>]
public partial class StampPair
{
    [StormColumn(ColumnType = ColumnType.ConcurrencyCheck)]
    public int Version { get; set; }

    [StormColumn(Size = 50)]
    public string? Note { get; set; }
}

/// <summary>
/// Entity whose only concurrency check column lives inside its FlatObject property.
/// </summary>
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "ConcurrencyFlatEntities", DisplayName = "ConcurrencyFlatEntity", ObjectType = DbObjectType.Table)]
public partial class ConcurrencyFlatEntity
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public int Id { get; set; }

    [StormColumn(Size = 50)]
    public string Name { get; set; } = string.Empty;

    [StormColumn(SaveAs = SaveAs.FlatObject, ColumnName = "Stamp")]
    public StampPair Stamp { get; set; } = new();
}
