using System;

namespace AltaSoft.Storm.Attributes;

/// <summary>
/// Represents the different types of columns that can be defined in a database schema.
/// This enum allows for specifying various characteristics and constraints of database columns.
/// </summary>
[Flags]
public enum ColumnType
{
    /// <summary>
    /// A default column type, without any specific constraints or characteristics.
    /// This is the standard type for a column when no special behavior is needed.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Represents a column that is a primary key (or part of a primary key), uniquely identifying each record in the table.
    /// A primary key ensures the uniqueness of each row in a table.
    /// </summary>
    PrimaryKey = 1,

    /// <summary>
    /// The AutoIncrement attribute is used to specify that a column in a database table should automatically increment its value for each new record inserted.
    /// This is commonly used for primary key columns to generate unique identifiers automatically.
    /// </summary>
    AutoIncrement = 2,

    /// <summary>
    /// Specifies that a column should be included in a concurrency check when updating a database record.
    /// This is used to ensure that the record has not been modified by another transaction before the update is applied.
    /// </summary>
    ConcurrencyCheck = 4,

    /// <summary>
    /// Specifies that a column is used for optimistic concurrency control.
    /// It is a binary value that is automatically updated by the database whenever a row is modified.
    /// It is used to detect conflicts when multiple users are trying to modify the same row simultaneously.
    /// </summary>
    RowVersion = 8,

    /// <summary>
    /// Indicates that the column in a database table has a default value.
    /// This is used when the database should automatically assign a default value if none is provided.
    /// </summary>
    HasDefaultValue = 16,

    /// <summary>
    /// Represents a conditional terminator for a column in a database table.
    /// If value of this column is true, the reading of the properties from <see cref="StormDbDataReader"/> will stop and following properties will not be initialized.
    /// </summary>
    ConditionalTerminator = 32,

    /// <summary>
    /// Represents a primary key column with an auto-increment feature,
    /// where the database automatically assigns a unique value when a new record is inserted.
    /// This is a combination of the PrimaryKey and AutoIncrement flags, for convenience.
    /// </summary>
    PrimaryKeyAutoIncrement = PrimaryKey | AutoIncrement
}
