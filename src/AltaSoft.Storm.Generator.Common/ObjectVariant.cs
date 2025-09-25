namespace AltaSoft.Storm.Generator.Common;

/// <summary>
/// Represents an object variant with properties for object name, update mode, and bind object data.
/// </summary>
public sealed class ObjectVariant
{
    /// <summary>
    /// Gets the unquoted table name.
    /// </summary>
    public string ObjectName { get; }

    /// <summary>
    /// Gets the display name of an object.
    /// </summary>
    public string DisplayName => BindObjectData.DisplayName ?? ObjectName;

    /// <summary>
    /// Gets update mode for the object.
    /// </summary>
    public DupUpdateMode UpdateMode { get; }

    /// <summary>
    /// Gets the Virtual View Sql.
    /// </summary>
    public string VirtualViewSql { get; }
    
    /// <summary>
    /// Gets the BindObjectData object used to bind data to a table.
    /// </summary>
    public BindObjectData BindObjectData { get; }

    public ObjectVariant(string objectName, DupUpdateMode updateMode, string? virtualViewSql, BindObjectData bindObjectData)
    {
        ObjectName = objectName;
        UpdateMode = updateMode;
        VirtualViewSql = virtualViewSql ?? "";
        BindObjectData = bindObjectData;
    }
}
