namespace AltaSoft.Storm;

/// <summary>
/// Represents a scalar value returned from a database query, including information about row existence and value presence.
/// </summary>
/// <typeparam name="T">The type of the scalar value.</typeparam>
public readonly struct DbScalar<T> // may be default for value types when HasValue is false
{
    /// <summary>
    /// Gets a value indicating whether a row was found in the database query.
    /// </summary>
    public bool RowFound { get; }

    /// <summary>
    /// Gets a value indicating whether the scalar value is present (not null or missing).
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// Gets the scalar value returned from the database query, or <c>null</c> if not present.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbScalar{T}"/> struct.
    /// </summary>
    /// <param name="rowFound">Indicates whether a row was found in the query result.</param>
    /// <param name="hasValue">Indicates whether the value is present.</param>
    /// <param name="value">The scalar value returned from the query.</param>
    public DbScalar(bool rowFound, bool hasValue, T? value)
    {
        RowFound = rowFound;
        HasValue = hasValue;
        Value = value;
    }

    /// <summary>
    /// Attempts to get the scalar value if present.
    /// </summary>
    /// <param name="value">
    /// When this method returns, contains the scalar value if <see cref="HasValue"/> is <c>true</c>;
    /// otherwise, the default value for the type of the <paramref name="value"/> parameter.
    /// </param>
    /// <returns>
    /// <c>true</c> if the value is present; otherwise, <c>false</c>.
    /// </returns>
    public bool TryGet(out T value)
    {
        if (HasValue)
        {
            value = Value!;
            return true;
        }
        value = default!;
        return false;
    }
}
