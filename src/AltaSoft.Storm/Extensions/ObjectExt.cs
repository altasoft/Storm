using System;

namespace AltaSoft.Storm.Extensions;

/// <summary>
/// Provides extensions for object types.
/// </summary>
public static class ObjectExt
{
    /// <summary>
    /// Converts a database value to a specified type.
    /// </summary>
    /// <typeparam name="TResult">The type to convert the database value to.</typeparam>
    /// <param name="dbValue">The database value to convert. Can be null or of type <see cref="DBNull"/>.</param>
    /// <returns>
    /// The converted value of type <typeparamref name="TResult"/>.
    /// Returns the default value of <typeparamref name="TResult"/> if <paramref name="dbValue"/> is null or of type <see cref="DBNull"/>.
    /// </returns>
    /// <exception cref="InvalidCastException">Thrown if the conversion cannot be performed.</exception>
    public static TResult GetDbValue<TResult>(this object? dbValue)
    {
        return dbValue switch
        {
            null => default!, // Using the null-forgiving operator here since default(TResult) can be null.
            DBNull => default!,
            not null => (TResult)dbValue
        };
    }
}
