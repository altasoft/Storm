//using System;
//using System.Data.Common;
//using System.Globalization;
//using System.Runtime.CompilerServices;

//namespace AltaSoft.StormManager.Extensions;

///// <summary>
///// Provides extension methods for DbDataReader for Get methods.
///// Type safe version, Uses AsXXX methods
///// </summary>
//public static partial class DbDataReaderExt
//{
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    private static TValue? AsNullable<TValue>(this DbDataReader self, int idx, Func<object, TValue> convert) where TValue : struct
//    {
//        return self.IsDBNull(idx) ? default(TValue?) : convert(self.GetValue(idx));
//    }

//    #region String

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static string? AsStringOrNull(this DbDataReader self, int idx)
//    {
//        return self.IsDBNull(idx) ? null : self.GetValue(idx).ToString();
//    }

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static string AsString(this DbDataReader self, int idx)
//    {
//        return self.IsDBNull(idx) ? string.Empty : self.GetValue(idx).ToString() ?? string.Empty;
//    }

//    #endregion String

//    #region Boolean

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static bool AsBoolean(this DbDataReader self, int idx) => Convert.ToBoolean(self[idx], CultureInfo.InvariantCulture);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static bool? AsBooleanOrNull(this DbDataReader self, int idx) => self.AsNullable(idx, x => Convert.ToBoolean(x, CultureInfo.InvariantCulture));

//    #endregion Boolean

//    #region Byte

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static byte AsByte(this DbDataReader self, int idx) => Convert.ToByte(self[idx], CultureInfo.InvariantCulture);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static byte? AsByteOrNull(this DbDataReader self, int idx) => self.AsNullable(idx, x => Convert.ToByte(x, CultureInfo.InvariantCulture));

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static sbyte AsSByte(this DbDataReader self, int idx) => (sbyte)Convert.ToByte(self[idx], CultureInfo.InvariantCulture);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static sbyte? AsSByteOrNull(this DbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.AsSByte(idx);

//    #endregion Byte

//    #region Int16

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static short AsInt16(this DbDataReader self, int idx) => Convert.ToInt16(self[idx], CultureInfo.InvariantCulture);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static short? AsInt16OrNull(this DbDataReader self, int idx) => self.AsNullable(idx, x => Convert.ToInt16(x, CultureInfo.InvariantCulture));

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static ushort AsUInt16(this DbDataReader self, int idx) => Convert.ToUInt16(self[idx], CultureInfo.InvariantCulture);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static ushort? AsUInt16OrNull(this DbDataReader self, int idx) => self.AsNullable(idx, x => Convert.ToUInt16(x, CultureInfo.InvariantCulture));

//    #endregion Int16

//    #region Int32

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static int AsInt32(this DbDataReader self, int idx) => Convert.ToInt32(self[idx], CultureInfo.InvariantCulture);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static int? AsInt32OrNull(this DbDataReader self, int idx) => self.AsNullable(idx, x => Convert.ToInt32(x, CultureInfo.InvariantCulture));

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static uint AsUInt32(this DbDataReader self, int idx) => Convert.ToUInt32(self[idx], CultureInfo.InvariantCulture);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static uint? AsUInt32OrNull(this DbDataReader self, int idx) => self.AsNullable(idx, x => Convert.ToUInt32(x, CultureInfo.InvariantCulture));

//    #endregion Int32

//    #region Int64

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static long AsInt64(this DbDataReader self, int idx) => Convert.ToInt64(self[idx], CultureInfo.InvariantCulture);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static long? AsInt64OrNull(this DbDataReader self, int idx) => self.AsNullable(idx, x => Convert.ToInt64(x, CultureInfo.InvariantCulture));

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static ulong AsUInt64(this DbDataReader self, int idx) => Convert.ToUInt64(self[idx], CultureInfo.InvariantCulture);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static ulong? AsUInt64OrNull(this DbDataReader self, int idx) => self.AsNullable(idx, x => Convert.ToUInt64(x, CultureInfo.InvariantCulture));

//    #endregion Int64

//    #region Decimal

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static decimal AsDecimal(this DbDataReader self, int idx) => Convert.ToDecimal(self[idx], CultureInfo.InvariantCulture);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static decimal? AsDecimalOrNull(this DbDataReader self, int idx) => self.AsNullable(idx, x => Convert.ToDecimal(x, CultureInfo.InvariantCulture));

//    #endregion Decimal

//    #region Double

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static double AsDouble(this DbDataReader self, int idx) => Convert.ToDouble(self[idx], CultureInfo.InvariantCulture);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static double? AsDoubleOrNull(this DbDataReader self, int idx) => self.AsNullable(idx, x => Convert.ToDouble(x, CultureInfo.InvariantCulture));

//    #endregion Double

//    #region Float

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static float AsFloat(this DbDataReader self, int idx) => Convert.ToSingle(self[idx], CultureInfo.InvariantCulture);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static float? AsFloatOrNull(this DbDataReader self, int idx) => self.AsNullable(idx, x => Convert.ToSingle(x, CultureInfo.InvariantCulture));

//    #endregion Float

//    #region DateTime

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    private static DateTime ConvertToLocalDateTime(object input) =>
//        new(Convert.ToDateTime(input, CultureInfo.InvariantCulture).Ticks, DateTimeKind.Local);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static DateTime AsDateTime(this DbDataReader self, int idx) => ConvertToLocalDateTime(self[idx]);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static DateTime? AsDateTimeOrNull(this DbDataReader self, int idx) => self.AsNullable(idx, ConvertToLocalDateTime);

//    #endregion DateTime

//    #region Date

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    private static DateOnly ConvertToDate(object input) => DateOnly.FromDateTime(Convert.ToDateTime(input, CultureInfo.InvariantCulture));

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static DateOnly AsDate(this DbDataReader self, int idx) => ConvertToDate(self[idx]);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static DateOnly? AsDateOrNull(this DbDataReader self, int idx) => self.AsNullable(idx, ConvertToDate);

//    #endregion Date

//    #region Time

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    private static TimeOnly ConvertToTimeOnly(object input) => TimeOnly.FromDateTime(Convert.ToDateTime(input, CultureInfo.InvariantCulture));

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static TimeOnly AsTime(this DbDataReader self, int idx) => ConvertToTimeOnly(self[idx]);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static TimeOnly? AsTimeOrNull(this DbDataReader self, int idx) => self.AsNullable(idx, ConvertToTimeOnly);

//    #endregion Time

//    #region TimeSpan

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    private static TimeSpan ConvertToTimeSpan(object input) => (TimeSpan)input;

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static TimeSpan AsTimeSpan(this DbDataReader self, int idx) => ConvertToTimeSpan(self[idx]);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static TimeSpan? AsTimeSpanOrNull(this DbDataReader self, int idx) => self.AsNullable(idx, ConvertToTimeSpan);

//    #endregion TimeSpan

//    #region Bytes

//    public static byte[] AsBytes(this DbDataReader self, int idx)
//    {
//        if (self.IsDBNull(idx))
//            return Array.Empty<byte>();

//        var len = self.GetBytes(idx, 0, null, 0, 0);
//        var bytes = new byte[len];
//        self.GetBytes(idx, 0, bytes, 0, (int)len);
//        return bytes;
//    }

//    public static byte[]? AsBytesOrNull(this DbDataReader self, int idx)
//    {
//        if (self.IsDBNull(idx))
//            return null;

//        var len = self.GetBytes(idx, 0, null, 0, 0);
//        var bytes = new byte[len];
//        self.GetBytes(idx, 0, bytes, 0, (int)len);
//        return bytes;
//    }

//    #endregion Bytes

//    #region Guid

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Guid AsGuid(this DbDataReader self, int idx) => self.IsDBNull(idx) ? Guid.Empty : self.GetGuid(idx);

//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    public static Guid? AsGuidOrNull(this DbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetGuid(idx);

//    #endregion Guid
//}
