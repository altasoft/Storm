using System;

namespace AltaSoft.Storm;

/// <summary>
/// Defines flags for ORM column behaviors, indicating the allowed operations on the column.
/// These flags can be combined to represent multiple behaviors.
/// </summary>
[Flags]
public enum StormColumnFlags
{
    /// <summary>
    /// Indicates no special behaviors or permissions for the column.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates the column is a key, which could be a primary key or part of a composite key.
    /// </summary>
    Key = 1,

    /// <summary>
    /// Indicates the column can be included in SELECT queries.
    /// </summary>
    CanSelect = 2,

    /// <summary>
    /// Indicates the column can be included in INSERT operations.
    /// </summary>
    CanInsert = 4,

    /// <summary>
    /// Indicates the column can be included in UPDATE operations.
    /// </summary>
    CanUpdate = 8,

    /// <summary>
    /// Indicates the column is for optimistic concurrency check in UPDATE operations.
    /// </summary>
    ConcurrencyCheck = 16,

    /// <summary>
    /// Specifies whether the column is auto-incremented.
    /// </summary>
    AutoIncrement = 32,

    /// <summary>
    /// Specifies whether the column is a rowversion (timestamp) column.
    /// </summary>
    RowVersion = 64
}
