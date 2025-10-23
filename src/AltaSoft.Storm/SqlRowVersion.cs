using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace AltaSoft.Storm;

/// <summary>
/// Represents a SQL rowversion value (timestamp, 8 bytes).
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct SqlRowVersion :
    IEquatable<SqlRowVersion>,
    IComparable,
    IComparable<SqlRowVersion>
#if NET7_0_OR_GREATER
    , IParsable<SqlRowVersion>
#endif
    , IConvertible
#if NET8_0_OR_GREATER
    , IUtf8SpanFormattable
#endif
{
    private const int ByteLength = 8;

    /// <summary>
    /// Represents a static readonly <see cref="SqlRowVersion"/> object initialized with a value of 0.
    /// </summary>
    public static readonly SqlRowVersion Zero = new(0);

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly ulong _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlRowVersion"/> struct from a 64-bit unsigned integer value.
    /// </summary>
    /// <param name="value">The 64-bit unsigned integer value representing the rowversion (timestamp).</param>
    public SqlRowVersion(ulong value)
    {
        _value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlRowVersion"/> struct from a read-only span of 8 bytes (big-endian).
    /// </summary>
    /// <param name="value">A read-only span of 8 bytes representing the rowversion (timestamp) in big-endian order.</param>
    /// <exception cref="ArgumentException">Thrown if the length of <paramref name="value"/> is not 8.</exception>
    public SqlRowVersion(ReadOnlySpan<byte> value)
    {
        if (value.Length != ByteLength)
            throw new ArgumentException($"RowVersion (timestamp) must be {ByteLength} bytes.", nameof(value));
        _value = ((ulong)value[0] << 56) | ((ulong)value[1] << 48) | ((ulong)value[2] << 40) | ((ulong)value[3] << 32)
            | ((ulong)value[4] << 24) | ((ulong)value[5] << 16) | ((ulong)value[6] << 8) | value[7];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlRowVersion"/> struct from a byte array (big-endian).
    /// </summary>
    /// <param name="value">A byte array of length 8 representing the rowversion (timestamp) in big-endian order.</param>
    public SqlRowVersion(byte[] value) : this((ReadOnlySpan<byte>)value) { }

    /// <summary>
    /// Converts the rowversion (timestamp) value to a big-endian byte array.
    /// </summary>
    /// <returns>A byte array of length 8 representing the rowversion (timestamp) in big-endian order.</returns>
    public byte[] ToArray()
    {
        var arr = new byte[ByteLength];
        arr[0] = (byte)(_value >> 56);
        arr[1] = (byte)(_value >> 48);
        arr[2] = (byte)(_value >> 40);
        arr[3] = (byte)(_value >> 32);
        arr[4] = (byte)(_value >> 24);
        arr[5] = (byte)(_value >> 16);
        arr[6] = (byte)(_value >> 8);
        arr[7] = (byte)_value;
        return arr;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SqlRowVersion ts && Equals(ts);

    /// <summary>
    /// Indicates whether the current <see cref="SqlRowVersion"/> is equal to another <see cref="SqlRowVersion"/>.
    /// </summary>
    /// <param name="other">A <see cref="SqlRowVersion"/> to compare with this instance.</param>
    /// <returns><c>true</c> if the current instance is equal to <paramref name="other"/>; otherwise, <c>false</c>.</returns>
    public bool Equals(SqlRowVersion other) => _value == other._value;

    /// <inheritdoc/>
    public override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    /// Compares the current instance with another <see cref="SqlRowVersion"/> and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other.
    /// </summary>
    /// <param name="other">A <see cref="SqlRowVersion"/> to compare with this instance.</param>
    /// <returns>
    /// A value less than zero if this instance is less than <paramref name="other"/>;
    /// zero if this instance is equal to <paramref name="other"/>;
    /// greater than zero if this instance is greater than <paramref name="other"/>.
    /// </returns>
    public int CompareTo(SqlRowVersion other) => _value.CompareTo(other._value);

    /// <inheritdoc/>
    public int CompareTo(object? obj) => obj is SqlRowVersion ts ? CompareTo(ts) : 1;

    /// <summary>
    /// Determines whether two <see cref="SqlRowVersion"/> values are equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(SqlRowVersion left, SqlRowVersion right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="SqlRowVersion"/> values are not equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><c>true</c> if the values are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(SqlRowVersion left, SqlRowVersion right) => !left.Equals(right);

    /// <summary>
    /// Determines whether one <see cref="SqlRowVersion"/> is less than another.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public static bool operator <(SqlRowVersion left, SqlRowVersion right) => left._value < right._value;

    /// <summary>
    /// Determines whether one <see cref="SqlRowVersion"/> is greater than another.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public static bool operator >(SqlRowVersion left, SqlRowVersion right) => left._value > right._value;

    /// <summary>
    /// Determines whether one <see cref="SqlRowVersion"/> is less than or equal to another.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public static bool operator <=(SqlRowVersion left, SqlRowVersion right) => left._value <= right._value;

    /// <summary>
    /// Determines whether one <see cref="SqlRowVersion"/> is greater than or equal to another.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <c>false</c>.</returns>
    public static bool operator >=(SqlRowVersion left, SqlRowVersion right) => left._value >= right._value;

    /// <summary>
    /// Adds two <see cref="SqlRowVersion"/> values.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>The sum of <paramref name="a"/> and <paramref name="b"/>.</returns>
    public static SqlRowVersion operator +(SqlRowVersion a, SqlRowVersion b) => new(a._value + b._value);

    /// <summary>
    /// Subtracts one <see cref="SqlRowVersion"/> from another.
    /// </summary>
    /// <param name="a">The value to subtract from.</param>
    /// <param name="b">The value to subtract.</param>
    /// <returns>The result of <paramref name="a"/> minus <paramref name="b"/>.</returns>
    public static SqlRowVersion operator -(SqlRowVersion a, SqlRowVersion b) => new(a._value - b._value);

    /// <summary>
    /// Multiplies two <see cref="SqlRowVersion"/> values.
    /// </summary>
    /// <param name="a">The first value.</param>
    /// <param name="b">The second value.</param>
    /// <returns>The product of <paramref name="a"/> and <paramref name="b"/>.</returns>
    public static SqlRowVersion operator *(SqlRowVersion a, SqlRowVersion b) => new(a._value * b._value);

    /// <summary>
    /// Divides one <see cref="SqlRowVersion"/> by another.
    /// </summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    /// <returns>The quotient of <paramref name="a"/> divided by <paramref name="b"/>.</returns>
    public static SqlRowVersion operator /(SqlRowVersion a, SqlRowVersion b) => new(a._value / b._value);

    /// <summary>
    /// Computes the remainder after dividing one <see cref="SqlRowVersion"/> by another.
    /// </summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    /// <returns>The remainder after dividing <paramref name="a"/> by <paramref name="b"/>.</returns>
    public static SqlRowVersion operator %(SqlRowVersion a, SqlRowVersion b) => new(a._value % b._value);

    /// <summary>
    /// Implicitly converts a <see cref="ulong"/> value to a <see cref="SqlRowVersion"/>.
    /// </summary>
    /// <param name="value">The <see cref="ulong"/> value to convert.</param>
    /// <returns>A <see cref="SqlRowVersion"/> representing the specified value.</returns>
    public static implicit operator SqlRowVersion(ulong value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="SqlRowVersion"/> to a <see cref="ulong"/>.
    /// </summary>
    /// <param name="value">The <see cref="SqlRowVersion"/> value to convert.</param>
    /// <returns>The <see cref="ulong"/> value represented by the <see cref="SqlRowVersion"/>.</returns>
    public static implicit operator ulong(SqlRowVersion value) => value._value;

    /// <summary>
    /// Explicitly converts a byte array to a <see cref="SqlRowVersion"/>.
    /// </summary>
    /// <param name="value">The byte array to convert.</param>
    /// <returns>A <see cref="SqlRowVersion"/> representing the specified byte array.</returns>
    public static explicit operator SqlRowVersion(byte[] value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="SqlRowVersion"/> to a byte array.
    /// </summary>
    /// <param name="value">The <see cref="SqlRowVersion"/> value to convert.</param>
    /// <returns>A byte array representing the <see cref="SqlRowVersion"/>.</returns>
    public static implicit operator byte[](SqlRowVersion value) => value.ToArray();

#if NET7_0_OR_GREATER
    /// <summary>
    /// Parses a string representation of a SQL rowversion (timestamp).
    /// </summary>
    /// <param name="s">The string to parse. Can be a 16-character hex string (optionally prefixed with "0x") or a decimal ulong string.</param>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <returns>A <see cref="SqlRowVersion"/> parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="s"/> is null or whitespace.</exception>
    /// <exception cref="FormatException">Thrown if the string is not a valid rowversion (timestamp).</exception>
    public static SqlRowVersion Parse(string s, IFormatProvider? provider = null)
    {
        if (string.IsNullOrWhiteSpace(s)) throw new ArgumentNullException(nameof(s));
        s = s.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
        if (s.Length == 16) // hex
        {
            var bytes = new byte[ByteLength];
            for (var i = 0; i < ByteLength; i++)
                bytes[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
            return new SqlRowVersion(bytes);
        }
        else // try as ulong decimal
        {
            return new SqlRowVersion(ulong.Parse(s, provider));
        }
    }

    /// <summary>
    /// Tries to parse a string representation of a SQL rowversion (timestamp).
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <param name="result">When this method returns, contains the parsed <see cref="SqlRowVersion"/>, or <see cref="Zero"/> if parsing failed.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out SqlRowVersion result)
    {
        try
        {
            result = Parse(s!, provider);
            return true;
        }
        catch
        {
            result = Zero;
            return false;
        }
    }
#endif

    /// <summary>
    /// Returns a string representation of the rowversion (timestamp) as a 16-character uppercase hexadecimal value.
    /// </summary>
    /// <returns>A string representation of the rowversion (timestamp).</returns>
    public override string ToString() => _value.ToString("X16", CultureInfo.InvariantCulture);

#if NET8_0_OR_GREATER
    /// <summary>
    /// Tries to format the value of the current instance into the provided UTF-8 byte span.
    /// </summary>
    /// <param name="utf8Destination">The span in which to write the formatted value.</param>
    /// <param name="bytesWritten">When this method returns, contains the number of bytes written.</param>
    /// <param name="format">A span containing the format string.</param>
    /// <param name="provider">An object that supplies culture-specific formatting information.</param>
    /// <returns><c>true</c> if formatting succeeded; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        // Write as hex, e.g., "0123456789ABCDEF"
        return _value.TryFormat(utf8Destination, out bytesWritten, format, provider);
    }
#endif

    // IConvertible implementation (unchanged)
    /// <inheritdoc/>
    TypeCode IConvertible.GetTypeCode() => _value.GetTypeCode();
    /// <inheritdoc/>
    bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)_value).ToBoolean(provider);
    /// <inheritdoc/>
    byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)_value).ToByte(provider);
    /// <inheritdoc/>
    char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)_value).ToChar(provider);
    /// <inheritdoc/>
    DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)_value).ToDateTime(provider);
    /// <inheritdoc/>
    decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)_value).ToDecimal(provider);
    /// <inheritdoc/>
    double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)_value).ToDouble(provider);
    /// <inheritdoc/>
    short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)_value).ToInt16(provider);
    /// <inheritdoc/>
    int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)_value).ToInt32(provider);
    /// <inheritdoc/>
    long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)_value).ToInt64(provider);
    /// <inheritdoc/>
    sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)_value).ToSByte(provider);
    /// <inheritdoc/>
    float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)_value).ToSingle(provider);
    /// <inheritdoc/>
    string IConvertible.ToString(IFormatProvider? provider) => _value.ToString(provider);
    /// <inheritdoc/>
    object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)_value).ToType(conversionType, provider);
    /// <inheritdoc/>
    ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)_value).ToUInt16(provider);
    /// <inheritdoc/>
    uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)_value).ToUInt32(provider);
    /// <inheritdoc/>
    ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)_value).ToUInt64(provider);

    /// <summary>
    /// Returns the greater of two specified <see cref="SqlRowVersion"/> values.
    /// </summary>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>The greater value of <paramref name="a"/> and <paramref name="b"/>.</returns>
    public static SqlRowVersion Max(SqlRowVersion a, SqlRowVersion b) => a > b ? a : b;

    /// <summary>
    /// Returns the lesser of two specified <see cref="SqlRowVersion"/> values.
    /// </summary>
    /// <param name="a">The first value to compare.</param>
    /// <param name="b">The second value to compare.</param>
    /// <returns>The lesser value of <paramref name="a"/> and <paramref name="b"/>.</returns>
    public static SqlRowVersion Min(SqlRowVersion a, SqlRowVersion b) => a < b ? a : b;
}
