using System;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Represents the result of a stored procedure execution.
/// </summary>
public abstract record StormProcedureResult
{
    /// <summary>
    /// Gets or sets the number of rows affected by a procedure execution.
    /// </summary>
    public int RowsAffected { get; set; }

    /// <summary>
    /// Gets or sets the return value of the stored procedure.
    /// </summary>
    public int ReturnValue { get; set; }

    /// <summary>
    /// Gets or sets the exception that occurred during the procedure execution, if any.
    /// </summary>
    public Exception? Exception { get; set; }
}
