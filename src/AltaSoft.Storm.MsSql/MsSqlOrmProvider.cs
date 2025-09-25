using System;
using System.Data;
using System.Runtime.CompilerServices;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Interfaces;
using Microsoft.Data.SqlClient;

namespace AltaSoft.Storm;

/// <summary>
/// Provides an implementation of the <see cref="IOrmProvider"/> interface for Microsoft SQL Server.
/// This class includes functionalities and mappings specific to the SQL Server database.
/// </summary>
public class MsSqlOrmProvider : IOrmProvider
{
    /// <inheritdoc/>
    public string Description => "Microsoft SQL server";

    /// <inheritdoc/>
    public char QuoteCharacter => '[';

    /// <inheritdoc/>
    public int MaxSysNameLength => 128;

    /// <inheritdoc/>
    public SqlCommand CreateCommand(bool haveInputOutputParams)
    {
        var result = new SqlCommand();
        if (!haveInputOutputParams)
            result.EnableOptimizedParameterBinding = true;
        return result;
    }

    /// <inheritdoc/>
    public SqlBatchCommand CreateBatchCommand(bool haveInputOutputParams)
    {
        var result = new SqlBatchCommand();
        //if (!haveInputOutputParams)
        //    result.EnableOptimizedParameterBinding = true;
        return result;
    }

    /// <inheritdoc/>
    public Exception? HandleDbException(SqlException dbException)
    {
        var number = dbException.Number;
        return number switch
        {
            900001 => new StormDbConcurrencyException(dbException.Message, dbException),
            2627 => new StormPrimaryKeyViolationException(dbException.Message, dbException),
            2601 => new StormUniqueKeyViolationException(dbException.Message, dbException),
            547 => new StormForeignKeyViolationException(dbException.Message, dbException),
            _ => null
        };
    }

    /// <inheritdoc/>
    public string ToSqlDbType(UnifiedDbType dbType, int size, int precision, int scale)
    {
        return dbType.ToSqlDbTypeText(size, precision, scale);
    }

    /// <summary>
    /// Converts the given value to a database value.
    /// </summary>
    /// <param name="value">The value to be converted.</param>
    /// <returns>The converted database value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object ConvertToDbValue(object? value)
    {
        return value switch
        {
            null => DBNull.Value,
            DateOnly date => date.ToDateTime(TimeOnly.MinValue),
            TimeOnly time => new DateTime(time.Ticks),
            SqlRowVersion dbts => (byte[])dbts,
            SqlLogSequenceNumber lsn => (byte[])lsn,
            _ => value
        };
    }

    /// <inheritdoc/>
    public SqlParameter AddDbParameter(SqlCommand command, string parameterName, UnifiedDbType dbType, int size, object? value, ParameterDirection direction)
    {
        return AddParameter(command.Parameters, parameterName, dbType, size, value, direction);
    }

    /// <inheritdoc/>
    public SqlParameter AddDbParameter(SqlBatchCommand command, string parameterName, UnifiedDbType dbType, int size, object? value, ParameterDirection direction)
    {
        return AddParameter(command.Parameters, parameterName, dbType, size, value, direction);
    }

    private static SqlParameter AddParameter(SqlParameterCollection parameters, string parameterName, UnifiedDbType dbType, int size, object? value, ParameterDirection direction)
    {
        var sqlDbType = dbType.ToNativeDbType();

        var p = parameters.Add(parameterName, sqlDbType, size);
        p.Direction = direction;
        p.Value = ConvertToDbValue(value);
        return p;
    }
}
