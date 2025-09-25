using AltaSoft.Storm.Crud;
using Microsoft.Data.SqlClient;

namespace AltaSoft.Storm;

/// <summary>
/// Represents parameters specific to query operations in ORM (Object-Relational Mapping).
/// </summary>
public class DdlParameters
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryParameters"/> class with the specified Storm database connection.
    /// For use in DDL statements.
    /// </summary>
    /// <param name="connection">The Storm database connection.</param>
    /// <param name="transaction">The Storm db transaction</param>
    internal DdlParameters(SqlConnection connection, SqlTransaction? transaction = null)
    {
        Connection = connection;
        Transaction = transaction;
    }

    /// <summary>
    /// Gets the <see cref="SqlConnection"/> object representing the connection to the Storm database.
    /// </summary>
    public SqlConnection Connection { get; }

    /// <summary>
    /// The <see cref="SqlTransaction"/> in which SQL statements execute.
    /// This property allows the association of database operations with a particular transaction context.
    /// It can be null, indicating that operations are not transaction-bound.
    /// </summary>
    public SqlTransaction? Transaction { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the database connection should be closed after the query execution.
    /// When set to true, the connection will be closed after the operation.
    /// This is useful for managing database connection lifetimes in scenarios where connections are not managed automatically.
    /// </summary>
    public bool CloseConnection { get; set; }

    /// <summary>
    /// Specifies the wait time (in seconds) before terminating the attempt to execute a command and generating an error.
    /// This property can be used to set a custom command timeout duration.
    /// A null value indicates that the default timeout for the database provider will be used.
    /// </summary>
    public int? CommandTimeout { get; set; }
}
