using System.ComponentModel;

// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles

namespace AltaSoft.Storm.Interfaces;

/// <summary>
/// Represents an interface for objects that are data-bindable.
/// This interface is implemented by classes that are intended to be bound to data sources,
/// allowing for a consistent approach to data binding across different types of objects.
/// </summary>
public interface IDataBindable
{
    /// <summary>
    /// Method to be called before saving the object. Can be overridden to implement custom save logic.
    /// </summary>
    /// <param name="action">The type of save action being performed.</param>
    void BeforeSave(SaveAction action) { }

    /// <summary>
    /// <c>For Storm internal use only !!!</c>
    /// <br/>
    /// Gets the __loadingFlags value
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    uint? __GetLoadingFlags();

    /// <summary>
    /// <c>For Storm internal use only !!!</c>
    /// <br/>
    /// Gets an array of the object's column definitions and their corresponding values.
    /// </summary>
    /// <returns>An array of tuples, each containing an StormColumnDef and its corresponding value.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    (StormColumnDef column, object? value)[] __GetColumnValues();

    /// <summary>
    /// <c>For Storm internal use only !!!</c>
    /// <br/>
    /// Adds a detail row to a Storm column with the specified column definition and row object.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    void __AddDetailRow(StormColumnDef column, object row);

    /// <summary>
    /// Sets the auto-increment value for a specific index in the StormDbDataReader.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    void __SetAutoIncValue(StormDbDataReader dr, int idx);
}
