using System;
using System.Diagnostics;

namespace AltaSoft.Storm;

/// <summary>
/// Represents a SQL Server Log Sequence Number (LSN) as a 10-byte value.
/// Provides comparison, equality, and conversion operations.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public readonly struct SqlLogSequenceNumber :
    IEquatable<SqlLogSequenceNumber>,
    IComparable,
    IComparable<SqlLogSequenceNumber>
#if NET7_0_OR_GREATER
    , IParsable<SqlLogSequenceNumber>
#endif
{
    private const int LsnLength = 10;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private readonly byte _b0, _b1, _b2, _b3, _b4, _b5, _b6, _b7, _b8, _b9;

    /// <summary>
    /// Gets a zero-valued <see cref="SqlLogSequenceNumber"/>.
    /// </summary>
    public static readonly SqlLogSequenceNumber Zero = new([0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlLogSequenceNumber"/> struct from a span of bytes.
    /// </summary>
    /// <param name="bytes">A span containing exactly 10 bytes representing the LSN.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="bytes"/> is not exactly 10 bytes long.</exception>
    public SqlLogSequenceNumber(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != LsnLength)
            throw new ArgumentException($"An LSN must be {LsnLength} bytes.", nameof(bytes));
        _b0 = bytes[0]; _b1 = bytes[1]; _b2 = bytes[2]; _b3 = bytes[3]; _b4 = bytes[4];
        _b5 = bytes[5]; _b6 = bytes[6]; _b7 = bytes[7]; _b8 = bytes[8]; _b9 = bytes[9];
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlLogSequenceNumber"/> struct from a byte array.
    /// </summary>
    /// <param name="bytes">A byte array containing exactly 10 bytes representing the LSN.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="bytes"/> is not exactly 10 bytes long.</exception>
    public SqlLogSequenceNumber(byte[] bytes) : this(bytes.AsSpan()) { }

    /// <summary>
    /// Returns the LSN as a 10-byte array.
    /// </summary>
    /// <returns>A byte array representing the LSN.</returns>
    public byte[] ToArray() => [_b0, _b1, _b2, _b3, _b4, _b5, _b6, _b7, _b8, _b9];

    /// <summary>
    /// Determines whether this instance and another specified <see cref="SqlLogSequenceNumber"/> object have the same value.
    /// </summary>
    /// <param name="other">The LSN to compare to this instance.</param>
    /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(SqlLogSequenceNumber other) => CompareTo(other) == 0;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is SqlLogSequenceNumber lsn && Equals(lsn);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var a = ToArray();
        var hash = 17;
        foreach (var b in a)
            hash = hash * 31 + b;
        return hash;
    }

    /// <summary>
    /// Compares this instance to a specified <see cref="SqlLogSequenceNumber"/> and returns an indication of their relative values.
    /// </summary>
    /// <param name="other">An LSN to compare.</param>
    /// <returns>
    /// A signed integer that indicates the relative order of the objects being compared.
    /// Less than zero: This instance is less than <paramref name="other"/>.
    /// Zero: This instance is equal to <paramref name="other"/>.
    /// Greater than zero: This instance is greater than <paramref name="other"/>.
    /// </returns>
    public int CompareTo(SqlLogSequenceNumber other)
    {
        var a = this.ToArray();
        var b = other.ToArray();
        for (var i = 0; i < LsnLength; i++)
        {
            var cmp = a[i].CompareTo(b[i]);
            if (cmp != 0) return cmp;
        }
        return 0;
    }

    /// <summary>
    /// Compares this instance to a specified object and returns an indication of their relative values.
    /// </summary>
    /// <param name="obj">An object to compare, or <c>null</c>.</param>
    /// <returns>
    /// A signed integer that indicates the relative order of the objects being compared.
    /// </returns>
    public int CompareTo(object? obj) => obj is SqlLogSequenceNumber lsn ? CompareTo(lsn) : 1;

    /// <summary>
    /// Determines whether two specified <see cref="SqlLogSequenceNumber"/> values are equal.
    /// </summary>
    public static bool operator ==(SqlLogSequenceNumber x, SqlLogSequenceNumber y) => x.Equals(y);

    /// <summary>
    /// Determines whether two specified <see cref="SqlLogSequenceNumber"/> values are not equal.
    /// </summary>
    public static bool operator !=(SqlLogSequenceNumber x, SqlLogSequenceNumber y) => !x.Equals(y);

    /// <summary>
    /// Determines whether one specified <see cref="SqlLogSequenceNumber"/> is less than another.
    /// </summary>
    public static bool operator <(SqlLogSequenceNumber x, SqlLogSequenceNumber y) => x.CompareTo(y) < 0;

    /// <summary>
    /// Determines whether one specified <see cref="SqlLogSequenceNumber"/> is greater than another.
    /// </summary>
    public static bool operator >(SqlLogSequenceNumber x, SqlLogSequenceNumber y) => x.CompareTo(y) > 0;

    /// <summary>
    /// Determines whether one specified <see cref="SqlLogSequenceNumber"/> is less than or equal to another.
    /// </summary>
    public static bool operator <=(SqlLogSequenceNumber x, SqlLogSequenceNumber y) => x.CompareTo(y) <= 0;

    /// <summary>
    /// Determines whether one specified <see cref="SqlLogSequenceNumber"/> is greater than or equal to another.
    /// </summary>
    public static bool operator >=(SqlLogSequenceNumber x, SqlLogSequenceNumber y) => x.CompareTo(y) >= 0;

    /// <summary>
    /// Returns a hexadecimal string representation of the LSN.
    /// </summary>
    /// <returns>A 20-character hexadecimal string.</returns>
    public override string ToString()
    {
        var a = ToArray();
        return BitConverter.ToString(a).Replace("-", string.Empty);
    }

#if NET7_0_OR_GREATER
    /// <summary>
    /// Parses a string representation of an LSN.
    /// </summary>
    /// <param name="s">A string containing a 20-character hexadecimal LSN, optionally prefixed with "0x".</param>
    /// <param name="provider">An optional format provider (not used).</param>
    /// <returns>A <see cref="SqlLogSequenceNumber"/> parsed from the string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="s"/> is null or whitespace.</exception>
    /// <exception cref="FormatException">Thrown if <paramref name="s"/> is not exactly 20 hex characters.</exception>
    public static SqlLogSequenceNumber Parse(string s, IFormatProvider? provider = null)
    {
        if (string.IsNullOrWhiteSpace(s)) throw new ArgumentNullException(nameof(s));
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s.Substring(2);
        if (s.Length != 20) throw new FormatException("LSN must be exactly 20 hex characters for binary(10).");
        var bytes = new byte[LsnLength];
        for (var i = 0; i < LsnLength; i++)
            bytes[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
        return new SqlLogSequenceNumber(bytes);
    }

    /// <summary>
    /// Attempts to parse a string representation of an LSN.
    /// </summary>
    /// <param name="s">A string containing a 20-character hexadecimal LSN, optionally prefixed with "0x".</param>
    /// <param name="provider">An optional format provider (not used).</param>
    /// <param name="result">When this method returns, contains the parsed LSN if successful; otherwise, <see cref="Zero"/>.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? s, IFormatProvider? provider, out SqlLogSequenceNumber result)
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

    // Conversion

    /// <summary>
    /// Explicitly converts a byte array to a <see cref="SqlLogSequenceNumber"/>.
    /// </summary>
    /// <param name="arr">A byte array containing exactly 10 bytes representing the LSN.</param>
    /// <returns>A <see cref="SqlLogSequenceNumber"/> instance created from the byte array.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="arr"/> is not exactly 10 bytes long.</exception>
    public static explicit operator SqlLogSequenceNumber(byte[] arr) => new(arr);

    /// <summary>
    /// Implicitly converts a <see cref="SqlLogSequenceNumber"/> to a byte array.
    /// </summary>
    /// <param name="lsn">The <see cref="SqlLogSequenceNumber"/> to convert.</param>
    /// <returns>A byte array representing the LSN.</returns>
    public static implicit operator byte[](SqlLogSequenceNumber lsn) => lsn.ToArray();

    /// <summary>
    /// Returns the greater of two <see cref="SqlLogSequenceNumber"/> values.
    /// </summary>
    /// <param name="a">The first LSN to compare.</param>
    /// <param name="b">The second LSN to compare.</param>
    /// <returns>The greater of <paramref name="a"/> and <paramref name="b"/>.</returns>
    public static SqlLogSequenceNumber Max(SqlLogSequenceNumber a, SqlLogSequenceNumber b) => a > b ? a : b;
    /// <summary>
    /// Returns the lesser of two <see cref="SqlLogSequenceNumber"/> values.
    /// </summary>
    /// <param name="a">The first LSN to compare.</param>
    /// <param name="b">The second LSN to compare.</param>
    /// <returns>The lesser of <paramref name="a"/> and <paramref name="b"/>.</returns>
    public static SqlLogSequenceNumber Min(SqlLogSequenceNumber a, SqlLogSequenceNumber b) => a < b ? a : b;
}
