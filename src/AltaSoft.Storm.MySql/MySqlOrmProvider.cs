using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm;

public class MySqlOrmProvider : IOrmProvider
{
    // A mapping dictionary to convert DbType enumeration values to their equivalent MySqlDbType values.
    // This is used internally to ensure that data types are handled correctly when interacting with Microsoft SQL Server.
    private static readonly Dictionary<UnifiedDbType, MySqlDbType?> s_dbTypeSqlTypeMap = new()
    {
        { UnifiedDbType.Boolean, MySqlDbType.Bit },
        { UnifiedDbType.UInt8, MySqlDbType.UByte }, // UByte for 8-bit unsigned integer
        { UnifiedDbType.Int8, MySqlDbType.Byte }, // Byte for 8-bit signed integer
        { UnifiedDbType.UInt16, MySqlDbType.UInt16 }, // UInt16 for 16-bit unsigned integer
        { UnifiedDbType.Int16, MySqlDbType.Int16 },
        { UnifiedDbType.UInt32, MySqlDbType.UInt32 }, // UInt32 for 32-bit unsigned integer
        { UnifiedDbType.Int32, MySqlDbType.Int32 },
        { UnifiedDbType.UInt64, MySqlDbType.UInt64 }, // UInt64 for 64-bit unsigned integer
        { UnifiedDbType.Int64, MySqlDbType.Int64 },
        { UnifiedDbType.AnsiChar, MySqlDbType.VarChar }, // Using VarChar for single characters
        { UnifiedDbType.Char, MySqlDbType.VarChar }, // Using VarChar for Unicode characters
        { UnifiedDbType.AnsiString, MySqlDbType.VarChar },
        { UnifiedDbType.String, MySqlDbType.VarChar }, // Using VarChar for strings
        { UnifiedDbType.AnsiStringFixedLength, MySqlDbType.VarChar }, // Using VarChar for fixed-length non-Unicode strings
        { UnifiedDbType.StringFixedLength, MySqlDbType.VarChar }, // Using VarChar for fixed-length Unicode strings
        { UnifiedDbType.Currency, MySqlDbType.Decimal }, // Using Decimal for Currency
        { UnifiedDbType.Single, MySqlDbType.Float },
        { UnifiedDbType.Double, MySqlDbType.Double },
        { UnifiedDbType.Decimal, MySqlDbType.Decimal },
        //{ UnifiedDbType.VarNumeric, MySqlDbType.Decimal }, // Using Decimal for VarNumeric
        { UnifiedDbType.SmallDateTime, MySqlDbType.DateTime }, // Using DateTime for SmallDateTime
        { UnifiedDbType.DateTime, MySqlDbType.DateTime }, // Using DateTime for DateTime2
        { UnifiedDbType.DateTimeOffset, MySqlDbType.DateTime }, // DateTimeOffset not directly supported, using DateTime
        { UnifiedDbType.Date, MySqlDbType.Date },
        { UnifiedDbType.Time, MySqlDbType.Time },
        { UnifiedDbType.Guid, MySqlDbType.Guid }, // Using Guid type
        { UnifiedDbType.Xml, MySqlDbType.Text }, // Using Text for XML
        { UnifiedDbType.Json, MySqlDbType.JSON }, // JSON supported in MySQL
        { UnifiedDbType.AnsiText, MySqlDbType.Text }, // Using Text for AnsiText
        { UnifiedDbType.Text, MySqlDbType.Text }, // Using Text for Text
        { UnifiedDbType.Binary, MySqlDbType.Binary },
        { UnifiedDbType.Blob, MySqlDbType.Blob } // Using Blob for large binary data
    };

    public string Description => "MsSql";

    public char QuoteCharacter => '`';

    public int MaxSysNameLength => 64;

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
        if (!s_dbTypeSqlTypeMap.TryGetValue(dbType, out var mySqlDbType) || !mySqlDbType.HasValue)
            throw new StormException($"Unsupported database column type: UnifiedDbType.{dbType}");

        var p = ((MySqlCommand)command).Parameters.Add(paramName, mySqlDbType.Value, size);
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
