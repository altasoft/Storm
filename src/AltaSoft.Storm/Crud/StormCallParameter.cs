using System.Data;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Represents a parameter used in a database call within the Storm framework.
/// </summary>
/// <remarks>
/// This class encapsulates all the necessary details for a database parameter, 
/// including its type, direction, name, precision, scale, size, and value.
/// </remarks>
public sealed class StormCallParameter
{
    /// <summary>
    /// Gets the name of the parameter.
    /// </summary>
    public readonly string ParameterName;

    /// <summary>
    /// Gets the database type of the parameter.
    /// </summary>
    public readonly UnifiedDbType DbType;

    /// <summary>
    /// Gets the value of the parameter. Can be null.
    /// </summary>
    public readonly object? Value;

    /// <summary>
    /// Gets the size of the parameter.
    /// </summary>
    public readonly int Size;

    /// <summary>
    /// Gets the precision of numeric parameters.
    /// </summary>
    public readonly byte Precision;

    /// <summary>
    /// Gets the scale of numeric parameters.
    /// </summary>
    public readonly byte Scale;

    /// <summary>
    /// Gets the direction of the parameter (Input, Output, InputOutput, ReturnValue).
    /// </summary>
    public readonly ParameterDirection Direction;

    /// <summary>
    /// Initializes a new instance of the <see cref="StormCallParameter"/> class with specified details.
    /// </summary>
    /// <param name="parameterName">The name of the parameter.</param>
    /// <param name="dbType">The database type of the parameter.</param>
    /// <param name="value">The value of the parameter. Can be null.</param>
    /// <param name="precision">The precision of numeric parameters.</param>
    /// <param name="scale">The scale of numeric parameters.</param>
    /// <param name="size">The size of the parameter.</param>
    /// <param name="direction">The direction of the parameter.</param>
    public StormCallParameter(string parameterName, UnifiedDbType dbType, object? value, int size = 0, byte precision = 0, byte scale = 0, ParameterDirection direction = ParameterDirection.Input)
    {
        ParameterName = parameterName;
        DbType = dbType;
        Value = value;
        Size = size;
        Precision = precision;
        Scale = scale;
        Direction = direction;
    }
}
