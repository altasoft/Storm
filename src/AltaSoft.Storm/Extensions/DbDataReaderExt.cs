using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.Helpers;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Extensions;

/// <summary>
/// Provides extension methods for StormDbDataReader for enhanced data access functionality, including serialization and object creation.
/// </summary>
public static partial class DbDataReaderExt
{
    #region Text

    /// <summary>
    /// Gets the ANSI text value at the specified index in the StormDbDataReader, or null if the value is DBNull.
    /// </summary>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the ANSI text value.</param>
    /// <returns>The ANSI text value at the specified index, or null if the value is DBNull.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetAnsiTextOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetString(idx);

    /// <summary>
    /// Gets the ANSI text value at the specified index in the StormDbDataReader.
    /// </summary>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the ANSI text value.</param>
    /// <returns>The ANSI text value at the specified index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetAnsiText(this StormDbDataReader self, int idx) => self.GetString(idx);

    /// <summary>
    /// Gets the text value at the specified index in the StormDbDataReader, or null if the value is DBNull.
    /// </summary>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the text value.</param>
    /// <returns>The text value at the specified index, or null if the value is DBNull.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetTextOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetString(idx);

    /// <summary>
    /// Gets the text value at the specified index in the StormDbDataReader.
    /// </summary>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the text value.</param>
    /// <returns>The text value at the specified index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetText(this StormDbDataReader self, int idx) => self.GetString(idx);

    #endregion Text

    #region Xml

    /// <summary>
    /// Gets the ANSI XML value at the specified index in the StormDbDataReader, or null if the value is DBNull.
    /// </summary>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the ANSI XML value.</param>
    /// <returns>The ANSI XML value at the specified index, or null if the value is DBNull.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetAnsiXmlOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetString(idx);

    /// <summary>
    /// Gets the ANSI XML value at the specified index in the StormDbDataReader.
    /// </summary>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the ANSI XML value.</param>
    /// <returns>The ANSI XML value at the specified index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetAnsiXml(this StormDbDataReader self, int idx) => self.GetString(idx);

    /// <summary>
    /// Gets the XML value at the specified index in the StormDbDataReader, or null if the value is DBNull.
    /// </summary>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the XML value.</param>
    /// <returns>The XML value at the specified index, or null if the value is DBNull.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetXmlOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetString(idx);

    /// <summary>
    /// Gets the XML value at the specified index in the StormDbDataReader.
    /// </summary>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the XML value.</param>
    /// <returns>The XML value at the specified index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetXml(this StormDbDataReader self, int idx) => self.GetString(idx);

    #endregion Xml

    #region Json

    /// <summary>
    /// Gets the ANSI JSON value at the specified index in the StormDbDataReader, or null if the value is DBNull.
    /// </summary>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the ANSI JSON value.</param>
    /// <returns>The ANSI JSON value at the specified index, or null if the value is DBNull.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetAnsiJsonOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetString(idx);

    /// <summary>
    /// Gets the ANSI JSON value at the specified index in the StormDbDataReader.
    /// </summary>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the ANSI JSON value.</param>
    /// <returns>The ANSI JSON value at the specified index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetAnsiJson(this StormDbDataReader self, int idx) => self.GetString(idx);

    /// <summary>
    /// Gets the JSON value at the specified index in the StormDbDataReader, or null if the value is DBNull.
    /// </summary>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the JSON value.</param>
    /// <returns>The JSON value at the specified index, or null if the value is DBNull.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetJsonOrNull(this StormDbDataReader self, int idx) => self.IsDBNull(idx) ? null : self.GetString(idx);

    /// <summary>
    /// Gets the JSON value at the specified index in the StormDbDataReader.
    /// </summary>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the JSON value.</param>
    /// <returns>The JSON value at the specified index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetJson(this StormDbDataReader self, int idx) => self.GetString(idx);

    #endregion Json

    #region As String

    /// <summary>
    /// Reads the value from the specified column as a string, converts it to the specified type, and returns the result as the specified type or null if the string is null.
    /// </summary>
    /// <typeparam name="TResult">The type to convert the string value to.</typeparam>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The zero-based column ordinal.</param>
    /// <returns>The value from the specified column converted to the specified type, or null if the string value is null.</returns>
    public static TResult? AsStringOrNull<TResult>(this StormDbDataReader self, int idx) where TResult : IParsable<TResult>
    {
        var s = self.GetStringOrNull(idx);
        return s is null ? default : TResult.Parse(s, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts the value at the specified index in the StormDbDataReader to String and returns it as the specified type.
    /// Throws a StormException if the value is null for a not null column.
    /// </summary>
    /// <typeparam name="TResult">The type to convert the String value to</typeparam>
    /// <param name="self">The StormDbDataReader instance</param>
    /// <param name="idx">The index of the value to convert to String</param>
    /// <returns>The String value converted to the specified type, or throws a StormException if the value is null for a not null column</returns>
    public static TResult AsString<TResult>(this StormDbDataReader self, int idx) where TResult : IParsable<TResult>
    {
        var r = self.AsStringOrNull<TResult>(idx);
        return r ?? throw new StormException("Null value for not null column");
    }

    #endregion As String

    #region As String Compressed

    /// <summary>
    /// Reads the value from the specified column as a string, converts it to the specified type, and returns the result as the specified type or null if the string is null.
    /// </summary>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The zero-based column ordinal.</param>
    /// <returns>The value from the specified column converted to the specified type, or null if the string value is null.</returns>
    public static string? AsStringCompressedOrNull(this StormDbDataReader self, int idx)
    {
        var b = self.GetBinaryOrNull(idx);
        return b is null ? null : SqlCompression.Decompress(b);
    }

    /// <summary>
    /// Converts the value at the specified index in the StormDbDataReader to String and returns it as the specified type.
    /// Throws a StormException if the value is null for a not null column.
    /// </summary>
    /// <param name="self">The StormDbDataReader instance</param>
    /// <param name="idx">The index of the value to convert to String</param>
    /// <returns>The String value converted to the specified type, or throws a StormException if the value is null for a not null column</returns>
    public static string AsStringCompressed(this StormDbDataReader self, int idx)
    {
        var r = self.AsStringCompressedOrNull(idx);
        return r ?? throw new StormException("Null value for not null column");
    }

    #endregion As String Compressed

    #region As Xml

    /// <summary>
    /// Converts the string value at the specified index in the StormDbDataReader to an object of type TResult by deserializing it from XML format using StormManager.FromXml method.
    /// Returns the deserialized object or null if the string value is null.
    /// </summary>
    /// <typeparam name="TResult">The type of the object to deserialize to.</typeparam>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the string value in the StormDbDataReader.</param>
    /// <returns>The deserialized object of type TResult or null if the string value is null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult? AsXmlOrNull<TResult>(this StormDbDataReader self, int idx) where TResult : class
    {
        var s = self.GetStringOrNull(idx);
        return s is null ? null : (TResult)StormManager.FromXml(s, typeof(TResult));
    }

    /// <summary>
    /// Converts the value at the specified index in the StormDbDataReader to XML and returns it as the specified type.
    /// If the value is null, throws a StormException with a message indicating null value for a not null column.
    /// </summary>
    /// <typeparam name="TResult">The type to which the XML value will be converted.</typeparam>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the value to convert to XML.</param>
    /// <returns>The XML value converted to the specified type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult AsXml<TResult>(this StormDbDataReader self, int idx) where TResult : class
    {
        var r = self.AsXmlOrNull<TResult>(idx);
        return r ?? throw new StormException("Null value for not null column");
    }

    #endregion As Xml

    #region As Xml Compressed

    /// <summary>
    /// Converts the string value at the specified index in the StormDbDataReader to an object of type TResult by deserializing it from XML format using StormManager.FromXml method.
    /// Returns the deserialized object or null if the string value is null.
    /// </summary>
    /// <typeparam name="TResult">The type of the object to deserialize to.</typeparam>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the string value in the StormDbDataReader.</param>
    /// <returns>The deserialized object of type TResult or null if the string value is null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult? AsXmlCompressedOrNull<TResult>(this StormDbDataReader self, int idx) where TResult : class
    {
        var b = self.GetBinaryOrNull(idx);
        if (b is null)
            return null;
        return (TResult)StormManager.FromXml(SqlCompression.Decompress(b), typeof(TResult));
    }

    /// <summary>
    /// Converts the value at the specified index in the StormDbDataReader to XML and returns it as the specified type.
    /// If the value is null, throws a StormException with a message indicating null value for a not null column.
    /// </summary>
    /// <typeparam name="TResult">The type to which the XML value will be converted.</typeparam>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the value to convert to XML.</param>
    /// <returns>The XML value converted to the specified type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult AsXmlCompressed<TResult>(this StormDbDataReader self, int idx) where TResult : class
    {
        var r = self.AsXmlCompressedOrNull<TResult>(idx);
        return r ?? throw new StormException("Null value for not null column");
    }

    #endregion As Xml

    #region As Json

    /// <summary>
    /// Reads the value from the specified column as a string, converts it to the specified type using StormManager.FromJson, and returns the result as the specified type or null if the string is null.
    /// </summary>
    /// <typeparam name="TResult">The type to convert the string value to.</typeparam>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The zero-based column ordinal.</param>
    /// <returns>The value from the specified column converted to the specified type, or null if the string value is null.</returns>
    public static TResult? AsJsonOrNull<TResult>(this StormDbDataReader self, int idx) where TResult : class
    {
        var s = self.GetStringOrNull(idx);
        return s is null ? null : (TResult)StormManager.FromJson(s, typeof(TResult));
    }

    /// <summary>
    /// Converts the value at the specified index in the StormDbDataReader to JSON and returns it as the specified type.
    /// Throws a StormException if the value is null for a not null column.
    /// </summary>
    /// <typeparam name="TResult">The type to convert the JSON value to</typeparam>
    /// <param name="self">The StormDbDataReader instance</param>
    /// <param name="idx">The index of the value to convert to JSON</param>
    /// <returns>The JSON value converted to the specified type, or throws a StormException if the value is null for a not null column</returns>
    public static TResult AsJson<TResult>(this StormDbDataReader self, int idx) where TResult : class
    {
        var r = self.AsJsonOrNull<TResult>(idx);
        return r ?? throw new StormException("Null value for not null column");
    }

    #endregion As Json

    #region As Json Compressed

    /// <summary>
    /// Reads the value from the specified column as a string, converts it to the specified type using StormManager.FromJson, and returns the result as the specified type or null if the string is null.
    /// </summary>
    /// <typeparam name="TResult">The type to convert the string value to.</typeparam>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The zero-based column ordinal.</param>
    /// <returns>The value from the specified column converted to the specified type, or null if the string value is null.</returns>
    public static TResult? AsJsonCompressedOrNull<TResult>(this StormDbDataReader self, int idx) where TResult : class
    {
        var b = self.GetBinaryOrNull(idx);
        if (b is null)
            return null;
        return (TResult)StormManager.FromJson(SqlCompression.Decompress(b), typeof(TResult));
    }

    /// <summary>
    /// Converts the value at the specified index in the StormDbDataReader to JSON and returns it as the specified type.
    /// Throws a StormException if the value is null for a not null column.
    /// </summary>
    /// <typeparam name="TResult">The type to convert the JSON value to</typeparam>
    /// <param name="self">The StormDbDataReader instance</param>
    /// <param name="idx">The index of the value to convert to JSON</param>
    /// <returns>The JSON value converted to the specified type, or throws a StormException if the value is null for a not null column</returns>
    public static TResult AsJsonCompressed<TResult>(this StormDbDataReader self, int idx) where TResult : class
    {
        var r = self.AsJsonCompressedOrNull<TResult>(idx);
        return r ?? throw new StormException("Null value for not null column");
    }

    #endregion As Json

    #region Flat Objects

    /// <summary>
    /// Converts the current row of the StormDbDataReader to a flat object of type TResult.
    /// </summary>
    /// <typeparam name="TResult">The type of object to convert the row to.</typeparam>
    /// <param name="self">The StormDbDataReader instance.</param>
    /// <param name="idx">The index of the current row.</param>
    /// <returns>The flat object of type TResult created from the current row of the StormDbDataReader.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TResult AsFlatObject<TResult>(this StormDbDataReader self, ref int idx) where TResult : IDataBindable
    {
        var ctrl = StormControllerCache.Get<TResult>(0);
        return (TResult)ctrl.Create(self, uint.MaxValue, ref idx);
    }

    #endregion Flat Objects
}
