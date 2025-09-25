using AltaSoft.Storm.Generator.Common;

namespace AltaSoft.Storm.Helpers;

/// <summary>
/// Extension methods for DupColumnType
/// </summary>
internal static class DupColumnTypeExt
{
    /// <summary>
    /// Determines if the given ColumnType is a key column.
    /// </summary>
    /// <param name="columnType">The DupColumnType to check.</param>
    /// <returns>True if the column is a key column, false otherwise.</returns>
    public static bool IsKey(this DupColumnType columnType) => (columnType & DupColumnType.PrimaryKey) != 0;
}
