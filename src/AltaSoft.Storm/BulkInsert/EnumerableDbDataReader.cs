using System;
using System.Collections.Generic;
using System.Data;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.BulkInsert;

internal sealed class EnumerableDbDataReader<T> : IDataReader
    where T : IDataBindable
{
    private readonly IEnumerable<T> _values;
    private IEnumerator<T>? _enumerator;

    private (StormColumnDef column, object? value)[]? _currentColumnValues;

    internal EnumerableDbDataReader(IEnumerable<T> values, int fieldCount)
    {
        _values = values;
        FieldCount = fieldCount;
    }

    /// <inheritdoc/>
    public bool Read()
    {
        _enumerator ??= _values.GetEnumerator();

        if (!_enumerator.MoveNext())
            return false;

        var t = _enumerator.Current;
        _currentColumnValues = t.__GetColumnValues();
        return true;
    }

    /// <inheritdoc/>
    public object GetValue(int i)
    {
        if (_currentColumnValues is null)
            throw new InvalidOperationException($"{nameof(_currentColumnValues)} is null");

        var (column, value) = _currentColumnValues[i];
        return column.GetValueForDbParameter(value, column.PropertySerializationType) ?? DBNull.Value;
    }

    /// <inheritdoc/>
    public int FieldCount { get; }

    /// <inheritdoc/>
    public object this[int i] => GetValue(i);

    /// <inheritdoc/>
    public object this[string name] => GetValue(GetOrdinal(name));

    /// <inheritdoc/>
    public bool IsClosed { get; private set; }

    /// <inheritdoc/>
    public int Depth => 0;

    /// <inheritdoc/>
    public bool GetBoolean(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public byte GetByte(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) =>
        throw new NotImplementedException();

    /// <inheritdoc/>
    public char GetChar(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) =>
        throw new NotImplementedException();

    /// <inheritdoc/>
    public IDataReader GetData(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public string GetDataTypeName(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public DateTime GetDateTime(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public decimal GetDecimal(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public double GetDouble(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Type GetFieldType(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public float GetFloat(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public Guid GetGuid(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public short GetInt16(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public int GetInt32(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public long GetInt64(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public string GetName(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public int GetOrdinal(string name) => throw new NotImplementedException();

    /// <inheritdoc/>
    public string GetString(int i) => throw new NotImplementedException();

    /// <inheritdoc/>
    public int GetValues(object[] values) => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool IsDBNull(int i) => GetValue(i) == DBNull.Value;

    /// <inheritdoc/>
    public int RecordsAffected => throw new NotImplementedException();

    public DataTable? GetSchemaTable() => throw new NotImplementedException();

    /// <inheritdoc/>
    public bool NextResult() => throw new NotImplementedException();

    /// <inheritdoc/>
    public void Close()
    {
        IsClosed = true;
    }

    public void Dispose()
    {
        _enumerator?.Dispose();
    }
}
