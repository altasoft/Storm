using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Interfaces;
using Npgsql;

namespace AltaSoft.Storm;

public class NpgSqlOrmProvider : IOrmProvider
{
    // A mapping dictionary to convert DbType enumeration values to their equivalent NpgsqlDbType values.
    // This is used internally to ensure that data types are handled correctly when interacting with Microsoft SQL Server.
    private static readonly Dictionary<UnifiedDbType, NpgsqlDbType?> s_dbTypeSqlTypeMap = new()
    {
        { UnifiedDbType.Boolean, NpgsqlDbType.Boolean },
        { UnifiedDbType.UInt8, NpgsqlDbType.Smallint }, // Using Smallint for 8-bit unsigned integer
        { UnifiedDbType.Int8, NpgsqlDbType.Smallint }, // No direct 8-bit signed integer, using Smallint
        { UnifiedDbType.UInt16, NpgsqlDbType.Integer }, // No unsigned 16-bit integer, using Integer
        { UnifiedDbType.Int16, NpgsqlDbType.Smallint },
        { UnifiedDbType.UInt32, NpgsqlDbType.Bigint }, // No unsigned 32-bit integer, using Bigint
        { UnifiedDbType.Int32, NpgsqlDbType.Integer },
        { UnifiedDbType.UInt64, NpgsqlDbType.Numeric }, // No native support for unsigned 64-bit, using Numeric
        { UnifiedDbType.Int64, NpgsqlDbType.Bigint },
        { UnifiedDbType.AnsiChar, NpgsqlDbType.Char }, // Using Char for single characters
        { UnifiedDbType.Char, NpgsqlDbType.Char }, // Using Char for Unicode characters
        { UnifiedDbType.AnsiString, NpgsqlDbType.Varchar }, // Using Varchar for Ansi strings
        { UnifiedDbType.String, NpgsqlDbType.Text }, // Using Text for Unicode strings
        { UnifiedDbType.AnsiStringFixedLength, NpgsqlDbType.Char }, // Using Char for fixed-length Ansi strings
        { UnifiedDbType.StringFixedLength, NpgsqlDbType.Char }, // Using Char for fixed-length Unicode strings
        { UnifiedDbType.Currency, NpgsqlDbType.Money }, // Using Money for Currency
        { UnifiedDbType.Single, NpgsqlDbType.Real },
        { UnifiedDbType.Double, NpgsqlDbType.Double },
        { UnifiedDbType.Decimal, NpgsqlDbType.Numeric },
        //{ UnifiedDbType.VarNumeric, NpgsqlDbType.Numeric }, // Using Numeric for VarNumeric
        { UnifiedDbType.SmallDateTime, NpgsqlDbType.Timestamp }, // Using Timestamp for SmallDateTime
        { UnifiedDbType.DateTime, NpgsqlDbType.Timestamp }, // Using Timestamp for DateTime2
        { UnifiedDbType.DateTimeOffset, NpgsqlDbType.TimestampTz }, // Using TimestampTz for DateTimeOffset
        { UnifiedDbType.Date, NpgsqlDbType.Date },
        { UnifiedDbType.Time, NpgsqlDbType.Time },
        { UnifiedDbType.Guid, NpgsqlDbType.Uuid }, // Using Uuid for GUID
        { UnifiedDbType.Xml, NpgsqlDbType.Xml }, // Using Xml for XML
        { UnifiedDbType.Json, NpgsqlDbType.Json }, // Using Json for JSON
        { UnifiedDbType.AnsiText, NpgsqlDbType.Text }, // Using Text for AnsiText
        { UnifiedDbType.Text, NpgsqlDbType.Text }, // Using Text for Text
        { UnifiedDbType.Binary, NpgsqlDbType.Bytea }, // Using Bytea for Binary data
        { UnifiedDbType.Blob, NpgsqlDbType.Bytea } // Using Bytea for Blob
    };

    public string Description => "Prostgre SQL";

    public char QuoteCharacter => '"';

    public int MaxSysNameLength => 63;

    public Func<UnifiedDbType, int, string> ToSqlDbTypeFunc => (dbType, _) =>
    {
        if (s_dbTypeSqlTypeMap.TryGetValue(dbType, out var type))
            return type.ToString()!;

        throw new StormException($"Unsupported DbType: {dbType}");
    };

    /// <summary>
    /// Converts the given value to a database value.
    /// </summary>
    /// <param name="value">The value to be converted.</param>
    /// <returns>The converted database value.</returns>
    private static object ConvertToDbValue(object? value)
    {
        return value switch
        {
            null => DBNull.Value,
#if NET6_0_OR_GREATER
            DateOnly date => date.ToDateTime(TimeOnly.MinValue),
            TimeOnly time => new DateTime(time.Ticks),
#endif
            _ => value
        };
    }

    public Func<DbCommand, string, UnifiedDbType, int, object?, DbParameter> AddDbParameterFunc => (command, paramName, dbType, size, value) =>
    {
        if (!s_dbTypeSqlTypeMap.TryGetValue(dbType, out var npgsqlDbType) || !npgsqlDbType.HasValue)
            throw new StormException($"Unsupported database column type: UnifiedDbType.{dbType}");

        var p = ((NpgsqlCommand)command).Parameters.Add(paramName, npgsqlDbType.Value, size);
        p.Value = ConvertToDbValue(value);
        return p;
    };

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<T?> FirstAsync<T>(DbConnection self, uint partialLoadFlags, Expression<Func<T, bool>>? whereExpression, IEnumerable<int>? orderBy, QueryParameters? queryParameters, CancellationToken cancellationToken = default) where T : IDataBindable
    {
        return self.FirstAsync(partialLoadFlags, whereExpression, orderBy, queryParameters, cancellationToken);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<List<T>> ListAsync<T>(DbConnection self, uint partialLoadFlags, Expression<Func<T, bool>>? whereExpression, IEnumerable<int>? orderBy, QueryParameters? queryParameters, CancellationToken cancellationToken = default) where T : IDataBindable
    {
        return self.ListAsync(partialLoadFlags, whereExpression, orderBy, queryParameters, cancellationToken);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAsyncEnumerable<T> StreamAsync<T>(DbConnection self, uint partialLoadFlags, Expression<Func<T, bool>>? whereExpression, IEnumerable<int>? orderBy,
        QueryParameters? queryParameters, CancellationToken cancellationToken = default) where T : IDataBindable
    {
        return self.StreamAsync(partialLoadFlags, whereExpression, orderBy, queryParameters, cancellationToken);
    }
}
