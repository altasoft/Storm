using System.Data.Common;
using System.Runtime.CompilerServices;

// ReSharper disable once CheckNamespace
namespace AltaSoft.Storm.Extensions;

public static partial class DbDataReaderExt
{
    #region Byte

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte GetSByte(this DbDataReader self, int idx) => (sbyte)self.GetInt16(idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static sbyte? GetSByteOrNull(this DbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetSByte(idx);

    #endregion Byte

    #region Int16

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort GetUInt16(this DbDataReader self, int idx) => (ushort)self.GetInt16(idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort? GetUInt16OrNull(this DbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetUInt16(idx);

    #endregion Int16

    #region Int32

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetUInt32(this DbDataReader self, int idx) => (uint)self.GetInt64(idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint? GetUInt32OrNull(this DbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetUInt32(idx);

    #endregion Int32

    #region Int64

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetUInt64(this DbDataReader self, int idx) => (ulong)self.GetInt64(idx);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong? GetUInt64OrNull(this DbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetUInt64(idx);

    #endregion Int64
}
