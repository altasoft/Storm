using System;
using System.Runtime.CompilerServices;
// ReSharper disable UnusedMember.Global

namespace AltaSoft.Storm.Extensions;

/// <summary>
/// Provides fast extension methods for <see cref="StormDbDataReader"/> to retrieve column values using GetXXX methods.
/// </summary>
public static partial class DbDataReaderExt
{
    #region String

    /// <summary>
    /// Gets the ANSI string value from the specified column index.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The ANSI string value at the specified column index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetAnsiString(this StormDbDataReader self, int idx) => self.GetString(idx);

    /// <summary>
    /// Gets the ANSI string value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The ANSI string value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetAnsiStringOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetString(idx);

    /// <summary>
    /// Gets the string value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The string value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetStringOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetString(idx);

    #endregion String

    #region Fixed Length String

    /// <summary>
    /// Gets the ANSI fixed-length string value from the specified column index.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The ANSI fixed-length string value at the specified column index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetAnsiStringFixedLength(this StormDbDataReader self, int idx) => self.GetString(idx);

    /// <summary>
    /// Gets the ANSI fixed-length string value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The ANSI fixed-length string value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetAnsiStringFixedLengthOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetString(idx);

    /// <summary>
    /// Gets the fixed-length string value from the specified column index.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The fixed-length string value at the specified column index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetStringFixedLength(this StormDbDataReader self, int idx) => self.GetString(idx);

    /// <summary>
    /// Gets the fixed-length string value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The fixed-length string value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetStringFixedLengthOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetString(idx);

    #endregion Fixed Length String

    #region Boolean

    /// <summary>
    /// Gets the boolean value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The boolean value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool? GetBooleanOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetBoolean(idx);

    #endregion Boolean

    #region UInt8

    /// <summary>
    /// Gets the UInt8 value from the specified column index.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The UInt8 value at the specified column index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetUInt8(this StormDbDataReader self, int idx) => self.GetByte(idx);

    /// <summary>
    /// Gets the UInt8 value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The UInt8 value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte? GetUInt8OrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetByte(idx);

    #endregion UInt8

    #region Int16

    /// <summary>
    /// Gets the Int16 value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The Int16 value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static short? GetInt16OrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetInt16(idx);

    #endregion Int16

    #region Int32

    /// <summary>
    /// Gets the Int32 value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The Int32 value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int? GetInt32OrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetInt32(idx);

    #endregion Int32

    #region Int64

    /// <summary>
    /// Gets the Int64 value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The Int64 value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long? GetInt64OrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetInt64(idx);

    #endregion Int64

    #region Currency

    /// <summary>
    /// Gets the Decimal value from the specified column index.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The Decimal value at the specified column index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal GetCurrency(this StormDbDataReader self, int idx) => self.GetDecimal(idx);

    /// <summary>
    /// Gets the Decimal value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The Decimal value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal? GetCurrencyOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetDecimal(idx);

    #endregion Currency

    #region Decimal

    /// <summary>
    /// Gets the Decimal value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The Decimal value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal? GetDecimalOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetDecimal(idx);

    #endregion Decimal

    #region Double

    /// <summary>
    /// Gets the Double value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The Double value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? GetDoubleOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetDouble(idx);

    #endregion Double

    #region Single

    /// <summary>
    /// Gets the Single value from the specified column index.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The Single value at the specified column index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetSingle(this StormDbDataReader self, int idx) => self.GetFloat(idx);

    /// <summary>
    /// Gets the Single value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The Single value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float? GetSingleOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetSingle(idx);

    #endregion Single

    #region DateTime

    /// <summary>
    /// Gets the <see cref="DateTime"/> value from the specified column index and sets its kind to <see cref="DateTimeKind.Local"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The <see cref="DateTime"/> value at the specified column index with <see cref="DateTimeKind.Local"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime GetLocalDateTime(this StormDbDataReader self, int idx) => DateTime.SpecifyKind(self.GetDateTime(idx), DateTimeKind.Local);

    /// <summary>
    /// Gets the <see cref="DateTime"/> value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The <see cref="DateTime"/> value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime? GetLocalDateTimeOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetLocalDateTime(idx);

    #endregion DateTime

    #region DateTime2

    /// <summary>
    /// Gets the <see cref="DateTime"/> value from the specified column index.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The <see cref="DateTime"/> value at the specified column index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime GetDateTime2(this StormDbDataReader self, int idx) => self.GetLocalDateTime(idx);

    /// <summary>
    /// Gets the <see cref="DateTime"/> value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The <see cref="DateTime"/> value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime? GetDateTime2OrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetLocalDateTime(idx);

    #endregion DateTime2

    #region SmallDateTime

    /// <summary>
    /// Gets the <see cref="DateTime"/> value from the specified column index.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The <see cref="DateTime"/> value at the specified column index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime GetSmallDateTime(this StormDbDataReader self, int idx) => self.GetLocalDateTime(idx);

    /// <summary>
    /// Gets the <see cref="DateTime"/> value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The <see cref="DateTime"/> value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime? GetSmallDateTimeOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetSmallDateTime(idx);

    #endregion SmallDateTime

    #region Date

    /// <summary>
    /// Gets the <see cref="DateOnly"/> value from the specified column index.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The <see cref="DateOnly"/> value at the specified column index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateOnly GetDate(this StormDbDataReader self, int idx) => DateOnly.FromDateTime(self.GetLocalDateTime(idx));

    /// <summary>
    /// Gets the <see cref="DateOnly"/> value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The <see cref="DateOnly"/> value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateOnly? GetDateOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetDate(idx);

    #endregion Date

    #region Time

    /// <summary>
    /// Gets the <see cref="TimeOnly"/> value from the specified column index.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The <see cref="TimeOnly"/> value at the specified column index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeOnly GetTime(this StormDbDataReader self, int idx) => TimeOnly.FromDateTime(self.GetLocalDateTime(idx));

    /// <summary>
    /// Gets the <see cref="TimeOnly"/> value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The <see cref="TimeOnly"/> value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeOnly? GetTimeOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetTime(idx);

    #endregion Time

    #region TimeSpan

    /// <summary>
    /// Gets the <see cref="TimeSpan"/> value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The <see cref="TimeSpan"/> value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan? GetTimeSpanOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : TimeSpan.FromTicks(self.GetLocalDateTime(idx).Ticks);

    #endregion TimeSpan

    #region Binary

    /// <summary>
    /// Gets the binary value from the specified column index.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The binary value at the specified column index.</returns>
    public static byte[] GetBinary(this StormDbDataReader self, int idx)
    {
        var len = self.GetBytes(idx, 0, null, 0, 0);
        var bytes = new byte[len];
        self.GetBytes(idx, 0, bytes, 0, (int)len);
        return bytes;
    }

    /// <summary>
    /// Gets the binary value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The binary value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    public static byte[]? GetBinaryOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetBinary(idx);

    #endregion Binary

    #region VarBinary

    /// <summary>
    /// Gets the varbinary value from the specified column index.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The varbinary value at the specified column index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetVarBinary(this StormDbDataReader self, int idx) => GetBinary(self, idx);

    /// <summary>
    /// Gets the varbinary value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The varbinary value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[]? GetVarBinaryOrNull(this StormDbDataReader self, int idx) => GetBinaryOrNull(self, idx);

    #endregion VarBinary

    #region Blob

    /// <summary>
    /// Gets the blob value from the specified column index.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The blob value at the specified column index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBlob(this StormDbDataReader self, int idx) => GetVarBinary(self, idx);

    /// <summary>
    /// Gets the blob value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The blob value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[]? GetBlobOrNull(this StormDbDataReader self, int idx) => GetBinaryOrNull(self, idx);

    #endregion Blob

    #region Guid

    /// <summary>
    /// Gets the <see cref="Guid"/> value from the specified column index, or <c>null</c> if the value is <see cref="DBNull"/>.
    /// </summary>
    /// <param name="self">The <see cref="StormDbDataReader"/> instance.</param>
    /// <param name="idx">The zero-based column index.</param>
    /// <returns>The <see cref="Guid"/> value at the specified column index, or <c>null</c> if <see cref="DBNull"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid? GetGuidOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetGuid(idx);

    #endregion Guid
}
