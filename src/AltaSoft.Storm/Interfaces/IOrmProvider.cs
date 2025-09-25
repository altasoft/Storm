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
    /// Creates a delegate for creating a DbCommand.
    /// </summary>
    /// <param name="haveInputOutputParams">A boolean value indicating whether the command will have input/output parameters.</param>
    /// <returns>A delegate that can be used to create a DbCommand object.</returns>
    StormDbCommand CreateCommand(bool haveInputOutputParams);

    /// <summary>
    /// Creates a delegate for creating a batch DbCommand.
    /// </summary>
    /// <param name="haveInputOutputParams">A boolean value indicating whether the command will have input/output parameters.</param>
    /// <returns>A delegate that can be used to create a DbBatchCommand object.</returns>
    StormDbBatchCommand CreateBatchCommand(bool haveInputOutputParams);

    /// <summary>
    /// Converts a <see cref="UnifiedDbType"/> to its corresponding SQL Server-specific data type representation, along with optional size, precision, and scale parameters.
    /// </summary>
    /// <param name="dbType">The UnifiedDbType to convert.</param>
    /// <param name="size">The size parameter for the SqlDbType.</param>
    /// <param name="precision">The precision parameter for the SqlDbType.</param>
    /// <param name="scale">The scale parameter for the SqlDbType.</param>
    /// <returns>A string representing the SQL Server data type.</returns>
    string ToSqlDbType(UnifiedDbType dbType, int size, int precision, int scale);

    /// <summary>
    /// Handles the specified DbException by analyzing the exception and returning a more specific exception type.
    /// </summary>
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
