using System.Runtime.CompilerServices;
using Microsoft.Data.SqlClient;

// ReSharper disable once CheckNamespace
namespace AltaSoft.Storm.Extensions;

/// <summary>
/// Provides extension methods for DbDataReader for retrieving various numeric types.
/// </summary>
public static class DbDataReaderExt
{
    #region Int8

    /// <summary>
    /// Retrieves the value of the specified column as a signed 8-bit integer.
    /// </summary>
    /// <param name="self">The DbDataReader instance.</param>
    /// <param name="idx">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column as a signed 8-bit integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte GetInt8(this SqlDataReader self, int idx) => (sbyte)self.GetInt16(idx);

    /// <summary>
    /// Retrieves the value of the specified column as a nullable signed 8-bit integer.
    /// </summary>
    /// <param name="self">The DbDataReader instance.</param>
    /// <param name="idx">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column as a nullable signed 8-bit integer, or null if the column is DBNull.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte? GetInt8OrNull(this SqlDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetInt8(idx);

    #endregion Int8

    #region Int16

    /// <summary>
    /// Retrieves the value of the specified column as an unsigned 16-bit integer.
    /// </summary>
    /// <param name="self">The DbDataReader instance.</param>
    /// <param name="idx">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column as an unsigned 16-bit integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort GetUInt16(this SqlDataReader self, int idx) => (ushort)self.GetInt16(idx);

    /// <summary>
    /// Retrieves the value of the specified column as a nullable unsigned 16-bit integer.
    /// </summary>
    /// <param name="self">The DbDataReader instance.</param>
    /// <param name="idx">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column as a nullable unsigned 16-bit integer, or null if the column is DBNull.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort? GetUInt16OrNull(this SqlDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetUInt16(idx);

    #endregion Int16

    #region Int32

    /// <summary>
    /// Retrieves the value of the specified column as an unsigned 32-bit integer.
    /// </summary>
    /// <param name="self">The DbDataReader instance.</param>
    /// <param name="idx">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column as an unsigned 32-bit integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetUInt32(this SqlDataReader self, int idx) => (uint)self.GetInt64(idx);

    /// <summary>
    /// Retrieves the value of the specified column as a nullable unsigned 32-bit integer.
    /// </summary>
    /// <param name="self">The DbDataReader instance.</param>
    /// <param name="idx">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column as a nullable unsigned 32-bit integer, or null if the column is DBNull.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint? GetUInt32OrNull(this SqlDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetUInt32(idx);

    #endregion Int32

    #region Int64

    /// <summary>
    /// Retrieves the value of the specified column as an unsigned 64-bit integer.
    /// </summary>
    /// <param name="self">The DbDataReader instance.</param>
    /// <param name="idx">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column as an unsigned 64-bit integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetUInt64(this SqlDataReader self, int idx) => (ulong)self.GetInt64(idx);

    /// <summary>
    /// Retrieves the value of the specified column as a nullable unsigned 64-bit integer.
    /// </summary>
    /// <param name="self">The DbDataReader instance.</param>
    /// <param name="idx">The zero-based column ordinal.</param>
    /// <returns>The value of the specified column as a nullable unsigned 64-bit integer, or null if the column is DBNull.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong? GetUInt64OrNull(this SqlDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetUInt64(idx);

    #endregion Int64
}
