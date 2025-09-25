using System;

namespace AltaSoft.Storm.Attributes;

/// <summary>
/// Attribute to indicate that an enum type can be converted to and from a string representation
/// using a custom converter implementing <see cref="IStormStringToEnumConverter{TEnum}"/>.
/// The generic parameter <typeparamref name="TEnum"/> specifies the enum type, and <typeparamref name="TConverter"/> specifies the converter type.
/// </summary>
/// <typeparam name="TEnum">
/// The enum type to be converted.
/// </typeparam>
/// <typeparam name="TConverter">
/// The type of the converter that implements <see cref="IStormStringToEnumConverter{TEnum}"/> and provides
/// the logic for converting between the enum and its string representation.
/// </typeparam>
/// <remarks>
/// This attribute can be applied to enum types to enable custom string conversion logic, such as
/// for database storage or serialization, with a specified maximum string length.
/// </remarks>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class StormStringEnumAttribute<TEnum, TConverter> : Attribute
    where TEnum : struct, Enum
    where TConverter : IStormStringToEnumConverter<TEnum>
{
    /// <summary>
    /// Gets the maximum allowed length of the string representation for the enum value.
    /// A value of <c>-1</c> indicates unlimited length.
    /// </summary>
    public int MaxLength { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StormStringEnumAttribute{TEnum, TConverter}"/> class
    /// with the specified maximum string length for the enum's string representation.
    /// </summary>
    /// <param name="maxLength">
    /// The maximum allowed length of the string representation for the enum value.
    /// </param>
    public StormStringEnumAttribute(int maxLength)
    {
        MaxLength = maxLength;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StormStringEnumAttribute{TEnum, TConverter}"/> class
    /// with unlimited maximum string length for the enum's string representation.
    /// </summary>
    public StormStringEnumAttribute()
    {
        MaxLength = -1;
    }
}
