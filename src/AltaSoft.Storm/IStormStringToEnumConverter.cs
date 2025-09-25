using System;

namespace AltaSoft.Storm;

/// <summary>
/// Defines methods for converting between enum values and their string representations for database storage and retrieval.
/// </summary>
/// <typeparam name="TEnum">
/// The enum type to convert. Must be a value type and an enumeration.
/// </typeparam>
public interface IStormStringToEnumConverter<TEnum> where TEnum : struct, Enum
{
    /// <summary>
    /// Converts the specified enum value to its string representation suitable for database storage.
    /// </summary>
    /// <param name="value">The enum value to convert.</param>
    /// <returns>
    /// A string representation of the provided <paramref name="value"/> for database storage.
    /// </returns>
    static abstract string ToDbString(TEnum value);

    /// <summary>
    /// Converts the specified string representation from the database back to the corresponding enum value.
    /// </summary>
    /// <param name="value">The string representation of the enum value.</param>
    /// <returns>
    /// The <typeparamref name="TEnum"/> value corresponding to the provided <paramref name="value"/>.
    /// </returns>
    static abstract TEnum FromDbString(string value);
}
