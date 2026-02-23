using System;
using System.Data;

namespace AltaSoft.Storm.Interfaces;

/// <summary>
/// Defines the contract for ORM providers, specifying the required functionalities and properties that an ORM provider must implement.
/// This interface includes methods and properties for handling database-specific operations and types.
/// </summary>
public interface IOrmProvider
{
    /// <summary>
    /// Gets the SQL dialect used by this provider.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets or sets the character used for quoting identifiers in SQL statements.
    /// </summary>
    char QuoteCharacter { get; }

    /// <summary>
    /// Gets the maximum length of system names like table and column names.
    /// </summary>
    int MaxSysNameLength { get; }

    /// <summary>
    /// Creates a new <see cref="StormDbCommand"/> instance for this provider.
    /// </summary>
    /// <param name="haveInputOutputParams">Indicates whether the command will have input/output parameters.</param>
    /// <returns>A new <see cref="StormDbCommand"/>.</returns>
    StormDbCommand CreateCommand(bool haveInputOutputParams);

    /// <summary>
    /// Creates a <see cref="StormDbBatch"/> object for grouping multiple commands.
    /// </summary>
    /// <returns>A <see cref="StormDbBatch"/> instance.</returns>
    StormDbBatch CreateBatch();

    /// <summary>
    /// Creates a new <see cref="StormDbBatchCommand"/> for this provider.
    /// </summary>
    /// <param name="haveInputOutputParams">Indicates whether the batch command will have input/output parameters.</param>
    /// <returns>A new <see cref="StormDbBatchCommand"/> instance.</returns>
    StormDbBatchCommand CreateBatchCommand(bool haveInputOutputParams);

    /// <summary>
    /// Converts a <see cref="UnifiedDbType"/> to a provider-specific SQL type representation,
    /// taking into account size, precision and scale where applicable.
    /// </summary>
    /// <param name="dbType">The <see cref="UnifiedDbType"/> to convert.</param>
    /// <param name="size">The size parameter for the resulting SQL type.</param>
    /// <param name="precision">The precision parameter for the resulting SQL type.</param>
    /// <param name="scale">The scale parameter for the resulting SQL type.</param>
    /// <returns>A string representing the provider-specific SQL data type.</returns>
    string ToSqlDbType(UnifiedDbType dbType, int size, int precision, int scale);

    /// <summary>
    /// Analyzes a <see cref="StormDbException"/> and returns a more specific exception instance when possible.
    /// Implementations may map database error codes to richer CLR exceptions.
    /// </summary>
    /// <param name="dbException">The low-level database exception to analyze.</param>
    /// <returns>A mapped <see cref="Exception"/> or <c>null</c> if no mapping is available.</returns>
    Exception? HandleDbException(StormDbException dbException);

    /// <summary>
    /// Adds a parameter to a <see cref="StormDbCommand"/> with the given parameter name, database type, size, and value.
    /// </summary>
    /// <param name="command">The database command to add the parameter to.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="dbType">The database type of the parameter.</param>
    /// <param name="size">The size of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <param name="direction">The direction of the parameter.</param>
    /// <returns>The added database parameter.</returns>
    StormDbParameter AddDbParameter(StormDbCommand command, string parameterName, UnifiedDbType dbType, int size, object? value, ParameterDirection direction);

    /// <summary>
    /// Adds a parameter to a <see cref="StormDbBatchCommand"/> with the given parameter name, database type, size, and value.
    /// </summary>
    /// <param name="command">The database batch command to add the parameter to.</param>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="dbType">The database type of the parameter.</param>
    /// <param name="size">The size of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <param name="direction">The direction of the parameter.</param>
    /// <returns>The added database parameter.</returns>
    StormDbParameter AddDbParameter(StormDbBatchCommand command, string parameterName, UnifiedDbType dbType, int size, object? value, ParameterDirection direction);
}
