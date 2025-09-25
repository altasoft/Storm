namespace AltaSoft.Storm.Generator.Common;

/// <summary>
/// Same as in AltaSoft.Storm
/// </summary>
public enum DupDbObjectType
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
