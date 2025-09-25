using System;

namespace AltaSoft.Storm.Generator.Common;

/// <summary>
/// Represents the different types of columns that can be defined in a database schema.
/// </summary>
[Flags]
public enum DupColumnType
{
    /// <summary>
    /// A default column type, without any specific constraints or characteristics.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Represents a column that is a primary key (or part of primary key), uniquely identifying each record in the table.
    /// </summary>
    PrimaryKey = 1,

    /// <summary>
    /// The AutoIncrement attribute is used to specify that a column in a database table should automatically increment its value for each new record inserted.
    /// </summary>
    /// <remarks>This column is read only</remarks>
    AutoIncrement = 2,

    /// <summary>
    /// Specifies that a column should be included in a concurrency check when updating a database record.
    /// </summary>
    ConcurrencyCheck = 4,

    /// <summary>
    /// Specifies that a column is used for optimistic concurrency control.
    /// It is a binary value that is automatically updated by the database whenever a row is modified.
    /// It is used to detect conflicts when multiple users are trying to modify the same row simultaneously.
    /// </summary>
    /// <remarks>This column is read only</remarks>
    RowVersion = 8,

    /// <summary>
    /// Specifies that whether the column in database table has a default value.
    /// </summary>
    HasDefaultValue = 16,

    /// <summary>
    /// Represents a conditional terminator for a column in a database table.
    /// </summary>
    ConditionalTerminator = 32,
}
