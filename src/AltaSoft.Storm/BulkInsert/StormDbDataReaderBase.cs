using System;
using System.Collections;
using System.Data.Common;
using System.Globalization;

namespace AltaSoft.Storm.BulkInsert;

internal abstract class StormDbDataReaderBase : DbDataReader
{
    private bool _isClosed;

    protected (StormColumnDef column, object? value)[]? CurrentColumnValues;

    protected StormDbDataReaderBase(int fieldCount)
    {
        FieldCount = fieldCount;
    }

    /// <inheritdoc/>
    public override object GetValue(int ordinal)
    {
        if (CurrentColumnValues is null)
            throw new InvalidOperationException($"{nameof(CurrentColumnValues)} is null");

        var (column, value) = CurrentColumnValues[ordinal];
        return column.GetValueForDbParameter(value, column.PropertySerializationType) ?? DBNull.Value;
    }

    /// <inheritdoc/>
    public override int FieldCount { get; }

    /// <inheritdoc/>
    public override object this[int ordinal] => GetValue(ordinal);

    /// <inheritdoc/>
    public override object this[string name] => GetValue(GetOrdinal(name));

    /// <inheritdoc/>
    public override bool HasRows => true;

    /// <inheritdoc/>
    public override bool IsClosed => _isClosed;

    /// <inheritdoc/>
    public override int Depth => 0;

    /// <inheritdoc/>
    public override bool GetBoolean(int ordinal)
    {
        return Convert.ToBoolean(GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override byte GetByte(int ordinal)
    {
        return Convert.ToByte(GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        var value = GetValue(ordinal);
        if (value is not byte[] bytes)
            throw new InvalidCastException($"Cannot convert {value.GetType().Name} to byte array");

        var available = bytes.Length - (int)dataOffset;
        var toCopy = Math.Min(length, available);
        if (buffer is not null)
            Array.Copy(bytes, dataOffset, buffer, bufferOffset, toCopy);
        return toCopy;
    }

    /// <inheritdoc/>
    public override char GetChar(int ordinal)
    {
        return Convert.ToChar(GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        var value = GetValue(ordinal);
        if (value is not string str)
            throw new InvalidCastException($"Cannot convert {value.GetType().Name} to string");

        var available = str.Length - (int)dataOffset;
        var toCopy = Math.Min(length, available);
        if (buffer != null)
            str.CopyTo((int)dataOffset, buffer, bufferOffset, toCopy);
        return toCopy;
    }

    /// <inheritdoc/>
    public override string GetDataTypeName(int ordinal) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override DateTime GetDateTime(int ordinal)
    {
        return DateTime.SpecifyKind(Convert.ToDateTime(GetValue(ordinal), CultureInfo.InvariantCulture), DateTimeKind.Local);
    }

    /// <inheritdoc/>
    public override decimal GetDecimal(int ordinal)
    {
        return Convert.ToDecimal(GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override double GetDouble(int ordinal)
    {
        return Convert.ToDouble(GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override Type GetFieldType(int ordinal) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override float GetFloat(int ordinal)
    {
        return Convert.ToSingle(GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override Guid GetGuid(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value is Guid guid)
            return guid;
        return Guid.Parse(value.ToString()!);
    }

    /// <inheritdoc/>
    public override short GetInt16(int ordinal)
    {
        return Convert.ToInt16(GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override int GetInt32(int ordinal)
    {
        return Convert.ToInt32(GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override long GetInt64(int ordinal)
    {
        return Convert.ToInt64(GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override string GetName(int ordinal) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override int GetOrdinal(string name) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override string GetString(int ordinal)
    {
        return Convert.ToString(GetValue(ordinal), CultureInfo.InvariantCulture)!;
    }

    /// <inheritdoc/>
    public override int GetValues(object[] values) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override bool IsDBNull(int ordinal) => GetValue(ordinal) == DBNull.Value;

    /// <inheritdoc/>
    public override int RecordsAffected => throw new NotImplementedException();

    /// <inheritdoc/>
    public override bool NextResult() => throw new NotImplementedException();

    /// <inheritdoc/>
    public override bool Read() => throw new NotImplementedException();

    /// <inheritdoc/>
    public override IEnumerator GetEnumerator() => throw new NotImplementedException();

    /// <inheritdoc/>
    public override void Close()
    {
        _isClosed = true;
    }
}
