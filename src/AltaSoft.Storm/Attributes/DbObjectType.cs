namespace AltaSoft.Storm.Attributes;

/// <summary>
/// Represents the types of database objects.
/// </summary>
public enum DbObjectType
{
    /// <summary>
    /// Represents a database table.
    /// </summary>
    Table,

    /// <summary>
    /// Represents a database view.
    /// </summary>
    View,

    /// <summary>
    /// Represents a virtual (defined in code) database view.
    /// </summary>
    VirtualView,

    /// <summary>
    /// Represents a custom SQL statement.
    /// </summary>
    CustomSqlStatement,

    /// <summary>
    /// Represents a stored procedure in the database.
    /// </summary>
    StoredProcedure,

    /// <summary>
    /// Represents a table-valued function in the database.
    /// </summary>
    TableValuedFunction,

    /// <summary>
    /// Represents a scalar-valued function in the database.
    /// </summary>
    ScalarValuedFunction
}
