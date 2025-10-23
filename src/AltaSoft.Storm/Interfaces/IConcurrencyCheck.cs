using System.ComponentModel;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

namespace AltaSoft.Storm.Interfaces;

/// <summary>
/// Defines an interface for objects that support change tracking, used in ORM (Object-Relational Mapping) scenarios.
/// </summary>
public interface IConcurrencyCheck
{
    /// <summary>
    /// <c>For Storm internal use only !!!</c>
    /// <br/>
    /// Gets an array of the object's column definitions and their corresponding saved values for concurrency columns.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    (StormColumnDef column, object? value)[] __ConcurrencyColumnValues() => [];
}
