// ReSharper disable InconsistentNaming
namespace AltaSoft.Storm.Generator.Common;

/// <summary>
/// Represents the different types of databases that can be used.
/// </summary>
public enum DatabaseType
{
    /// <summary>
    /// Represents an undefined database type.
    /// </summary>
    Undefined = 0,

    /// <summary>
    /// Represents a Microsoft SQL Server database.
    /// </summary>
    SQLServer = 3,

    /// <summary>
    /// Represents a SQLite database.
    /// </summary>
    SQLite = 4,

    /// <summary>
    /// Represents a PostgreSQL database.
    /// </summary>
    Npgsql = 5,

    /// <summary>
    /// Represents a MySQL database.
    /// </summary>
    Mysql = 6,

    /// <summary>
    /// Represents an Oracle database.
    /// </summary>
    Oracle = 7,

    /// <summary>
    /// Represents a Firebird database.
    /// </summary>
    Firebird = 10
}
