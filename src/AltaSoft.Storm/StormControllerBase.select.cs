using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Helpers;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm;

/// <summary>
/// Abstract base class for ORM (Object-Relational Mapping) controllers, providing common functionality for database operations.
/// </summary>
public abstract partial class StormControllerBase
{
    private readonly ConcurrentDictionary<(uint partialLoadFlags, string? tableAlias), string> _queryCache = new();

    /// <summary>
    /// Streams data asynchronously from a database connection.
    /// </summary>
    /// <typeparam name="T">The type of data to stream.</typeparam>
    /// <param name="queryParameters">The Query parameters.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of the streamed data.</returns>
    internal async IAsyncEnumerable<T> StreamAsync<T>(
        SelectQueryParameters<T> queryParameters,
        [EnumeratorCancellation] CancellationToken cancellationToken) where T : IDataBindable
    {
        var partialLoadFlags = queryParameters.PartialLoadFlags;
        (partialLoadFlags, var shouldLoadDetails) = GetPartialLoadingData(partialLoadFlags, queryParameters);

        if (shouldLoadDetails) // We cannot stream details
        {
            foreach (var row in await ListAsync(partialLoadFlags, shouldLoadDetails, queryParameters, cancellationToken).ConfigureAwait(false))
            {
                yield return row;
            }
        }
        else
        {
            var command = StormManager.CreateCommand(false);
            await using (command.ConfigureAwait(false))
            {
                var reader = await GenerateSqlAndExecuteReaderAsync(command, queryParameters, partialLoadFlags, shouldLoadDetails, false, cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false))
                {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var idx = 0;
                        var row = (T)Create(reader, partialLoadFlags, ref idx);

                        if (queryParameters.AutoStartChangeTracking && row is IChangeTrackable changeTrackable)
                            changeTrackable.StartChangeTracking();

                        yield return row;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Counts the number of records in the database table asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the data bindable object.</typeparam>
    /// <param name="queryParameters">The Query parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of records in the database table.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the connection is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the command cannot be created.</exception>
    /// <exception cref="StormDbException">Thrown when an error occurs during the execution of the SQL query.</exception>
    internal async Task<int> CountAsync<T>(
        SelectQueryParameters<T> queryParameters,
        CancellationToken cancellationToken) where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var callParamsStr = command.GenerateCallParameters(queryParameters.CallParameters, ObjectType == DbObjectType.CustomSqlStatement ? CallParameterType.CustomSqlStatement : CallParameterType.Function);

            var sb = StormManager.GetStringBuilderFromPool();

            var vCommand = new StormVirtualDbCommand(command);
            GenerateCountSql(vCommand, queryParameters, callParamsStr, sb);

            return await InternalExecuteScalarQueryAsync<int>(command, sb.ToStringAndReturnToPool(), queryParameters, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Checks if a record exists in the database asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the record.</typeparam>
    /// <param name="queryParameters">The Query parameters.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if a record exists, false otherwise.</returns>
    internal async Task<bool> ExistsAsync<T>(
        SelectQueryParameters<T> queryParameters,
        CancellationToken cancellationToken) where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var callParamsStr = command.GenerateCallParameters(queryParameters.CallParameters, ObjectType == DbObjectType.CustomSqlStatement ? CallParameterType.CustomSqlStatement : CallParameterType.Function);

            var sb = StormManager.GetStringBuilderFromPool();

            var vCommand = new StormVirtualDbCommand(command);
            GenerateExistsSql(vCommand, queryParameters, callParamsStr, sb);

            return await InternalExecuteScalarQueryAsync<int>(command, sb.ToStringAndReturnToPool(), queryParameters, cancellationToken).ConfigureAwait(false) != 0;
        }
    }

    #region Get

    /// <summary>
    /// Retrieves a record from the database based on the specified key values, or returns the default value if no record is found.
    /// </summary>
    /// <typeparam name="T">The type of the record to retrieve.</typeparam>
    /// <param name="queryParameters">The Query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The retrieved record, or the default value if no record is found.</returns>
    internal async Task<T?> FirstOrDefaultAsync<T>(
        SelectQueryParameters<T> queryParameters,
        CancellationToken cancellationToken) where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);
            var (commandBehavior, partialLoadFlags, shouldLoadDetails) = GenerateSelectSql(vCommand, queryParameters, true);

            var connection = queryParameters.Context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var reader = await command.ExecuteCommandReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);

            await using (reader.ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    return default;

                var idx = 0;
                var row = (T)Create(reader, partialLoadFlags, ref idx);

                if (queryParameters.AutoStartChangeTracking && row is IChangeTrackable changeTrackable)
                    changeTrackable.StartChangeTracking();

                if (!shouldLoadDetails)
                    return row;

                foreach (var column in ColumnDefs.GetDetailColumns(partialLoadFlags))
                {
                    Debug.Assert(column.DetailType is not null);

                    if (!await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
                        throw new StormException("Expected a result set for detail table.");

                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        idx = 0;
                        _ = __ReadKeyValue(reader, ref idx);

                        var detailRow = CreateDetailRow(column, reader, ref idx);

                        if (queryParameters.AutoStartChangeTracking && detailRow is IChangeTrackable detChangeTrackable)
                            detChangeTrackable.StartChangeTracking();

                        row.__AddDetailRow(column, detailRow);
                    }
                }
                return row;
            }
        }
    }

    /// <summary>
    /// Asynchronously retrieves a single scalar value from a database using the provided connection, column selector, optional where expression, order by criteria, query parameters, default value, and cancellation token.
    /// </summary>
    /// <typeparam name="T">The type of data entity being queried.</typeparam>
    /// <typeparam name="TColumn">The type of the scalar result being retrieved.</typeparam>
    /// <param name="columnSelector">Expression specifying the column to retrieve the scalar value from.</param>
    /// <param name="queryParameters">The Query parameters.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous task that represents the operation and returns the retrieved scalar value.</returns>
    internal async Task<DbScalar<TColumn>> GetColumnValueAsync<T, TColumn>(
        Expression<Func<T, TColumn>> columnSelector,
        SelectQueryParameters<T> queryParameters,
        CancellationToken cancellationToken = default)
        where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);
            var (commandBehavior, propertyName) = GetColumnValue(vCommand, columnSelector, queryParameters);

            var connection = queryParameters.Context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var reader = await command.ExecuteCommandReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    return new DbScalar<TColumn>(false, false, default);

                var idx = 0;
                var value = ReadSingleScalarValue(reader, propertyName, ref idx);
                return new DbScalar<TColumn>(true, value is not null, value is null ? default : (TColumn)value);
            }
        }
    }

    internal async Task<(TColumn1, TColumn2)?> GetColumnValuesAsync<T, TColumn1, TColumn2>(
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        SelectQueryParameters<T> queryParameters,
        CancellationToken cancellationToken) where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);
            var (commandBehavior, propertyName1, propertyName2) = GetColumnValues(vCommand, columnSelector1, columnSelector2, queryParameters);

            var connection = queryParameters.Context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var reader = await command.ExecuteCommandReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    return null;

                var idx = 0;
                var result1 = ReadSingleScalarValue(reader, propertyName1, ref idx).GetDbValue<TColumn1>();
                var result2 = ReadSingleScalarValue(reader, propertyName2, ref idx).GetDbValue<TColumn2>();

                return (result1, result2);
            }
        }
    }

    internal async Task<(TResult1, TResult2, TResult3)?> GetColumnValuesAsync<T, TResult1, TResult2, TResult3>(
        Expression<Func<T, TResult1>> columnSelector1,
        Expression<Func<T, TResult2>> columnSelector2,
        Expression<Func<T, TResult3>> columnSelector3,
        SelectQueryParameters<T> queryParameters,
        CancellationToken cancellationToken) where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);

        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);
            var (commandBehavior, propertyName1, propertyName2, propertyName3) = GetColumnValues(vCommand, columnSelector1, columnSelector2, columnSelector3, queryParameters);

            var connection = queryParameters.Context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var reader = await command.ExecuteCommandReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    return null;

                var idx = 0;
                var result1 = ReadSingleScalarValue(reader, propertyName1, ref idx).GetDbValue<TResult1>();
                var result2 = ReadSingleScalarValue(reader, propertyName2, ref idx).GetDbValue<TResult2>();
                var result3 = ReadSingleScalarValue(reader, propertyName3, ref idx).GetDbValue<TResult3>();

                return (result1, result2, result3);
            }
        }
    }

    #endregion Get

    #region List

    /// <summary>
    /// Retrieves a list of objects asynchronously from the database based on the provided parameters.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <param name="partialLoadFlags">Flags indicating which columns to partially load.</param>
    /// <param name="shouldLoadDetails">A flag indicating whether to load details for the retrieved data.</param>
    /// <param name="queryParameters">The Query parameters.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A list of objects retrieved from the database.</returns>
    internal async Task<List<T>> ListAsync<T>(
        uint partialLoadFlags,
        bool shouldLoadDetails,
        SelectQueryParameters<T> queryParameters,
        CancellationToken cancellationToken) where T : IDataBindable
    {
        var lookup = shouldLoadDetails ? new Dictionary<object, T>(16) : null;

        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var reader = await GenerateSqlAndExecuteReaderAsync(command, queryParameters, partialLoadFlags, shouldLoadDetails, false, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                var result = new List<T>(16);

                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var idx = 0;
                    var row = (T)Create(reader, partialLoadFlags, ref idx);

                    if (queryParameters.AutoStartChangeTracking && row is IChangeTrackable changeTrackable)
                        changeTrackable.StartChangeTracking();

                    if (shouldLoadDetails && row is IDataBindableWithKey rowWithKey)
                        lookup!.Add(rowWithKey.__GetKeyValue(), row);

                    result.Add(row);
                }

                if (!shouldLoadDetails)
                    return result;

                foreach (var column in ColumnDefs.GetDetailColumns(partialLoadFlags))
                {
                    Debug.Assert(column.DetailType is not null);

                    if (!await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
                        throw new StormException("Expected a result set for detail table.");

                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var idx = 0;
                        var key = __ReadKeyValue(reader, ref idx);

                        var detailRow = CreateDetailRow(column, reader, ref idx);

                        if (queryParameters.AutoStartChangeTracking && detailRow is IChangeTrackable detChangeTrackable)
                            detChangeTrackable.StartChangeTracking();

                        if (lookup!.TryGetValue(key, out var master))
                        {
                            master.__AddDetailRow(column, detailRow);
                        }
                    }
                }

                return result;
            }
        }
    }

    internal async Task<List<TColumn>> ListColumnValuesAsync<T, TColumn>(
        Expression<Func<T, TColumn>> columnSelector,
        SelectQueryParameters<T> queryParameters,
        CancellationToken cancellationToken) where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);
            var (commandBehavior, propertyName) = ListColumnValues(vCommand, columnSelector, queryParameters);

            var connection = queryParameters.Context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var reader = await command.ExecuteCommandReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                var result = new List<TColumn>(16);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var idx = 0;
                    result.Add(ReadSingleScalarValue(reader, propertyName, ref idx).GetDbValue<TColumn>());
                }
                return result;
            }
        }
    }

    internal async Task<List<(TColumn1, TColumn2)>> ListColumnValuesAsync<T, TColumn1, TColumn2>(
       Expression<Func<T, TColumn1>> columnSelector1,
       Expression<Func<T, TColumn2>> columnSelector2,
       SelectQueryParameters<T> queryParameters,
       CancellationToken cancellationToken) where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);
            var (commandBehavior, propertyName1, propertyName2) = ListColumnValues(vCommand, columnSelector1, columnSelector2, queryParameters);

            var connection = queryParameters.Context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var reader = await command.ExecuteCommandReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                var result = new List<(TColumn1, TColumn2)>(16);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var idx = 0;
                    var result1 = ReadSingleScalarValue(reader, propertyName1, ref idx).GetDbValue<TColumn1>();
                    var result2 = ReadSingleScalarValue(reader, propertyName2, ref idx).GetDbValue<TColumn2>();

                    result.Add((result1, result2));
                }
                return result;
            }
        }
    }

    internal async Task<List<(TColumn1, TColumn2, TColumn3)>> ListColumnValuesAsync<T, TColumn1, TColumn2, TColumn3>(
       Expression<Func<T, TColumn1>> columnSelector1,
       Expression<Func<T, TColumn2>> columnSelector2,
       Expression<Func<T, TColumn3>> columnSelector3,
       SelectQueryParameters<T> queryParameters,
       CancellationToken cancellationToken) where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);
            var (commandBehavior, propertyName1, propertyName2, propertyName3) = ListColumnValues(vCommand, columnSelector1, columnSelector2, columnSelector3, queryParameters);

            var connection = queryParameters.Context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var reader = await command.ExecuteCommandReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                var result = new List<(TColumn1, TColumn2, TColumn3)>(16);
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var idx = 0;
                    var result1 = ReadSingleScalarValue(reader, propertyName1, ref idx).GetDbValue<TColumn1>();
                    var result2 = ReadSingleScalarValue(reader, propertyName2, ref idx).GetDbValue<TColumn2>();
                    var result3 = ReadSingleScalarValue(reader, propertyName3, ref idx).GetDbValue<TColumn3>();

                    result.Add((result1, result2, result3));
                }
                return result;
            }
        }
    }

    #endregion List

    #region Private methods

    private (CommandBehavior commandBehavior, string propertyName) GetColumnValue<T, TColumn>(
        StormVirtualDbCommand command,
        Expression<Func<T, TColumn>> columnSelector,
        SelectQueryParameters<T> queryParameters) where T : IDataBindable
    {
        var propertyName = columnSelector.GetPropertyNameFromExpression();
        var columnDef = Array.Find(ColumnDefs, x => string.Equals(x.PropertyName, propertyName, StringComparison.Ordinal))
                        ?? throw new StormException($"Column '{propertyName}' not found in the column definitions.");

        var callParamsStr = command.GenerateCallParameters(queryParameters.CallParameters, ObjectType == DbObjectType.CustomSqlStatement ? CallParameterType.CustomSqlStatement : CallParameterType.Function);

        var sb = StormManager.GetStringBuilderFromPool();

        sb.Append("SELECT ").AppendLine(columnDef.ColumnName);
        sb.Append("FROM ").Append(QuotedObjectFullName);

        if (callParamsStr is not null)
        {
            sb.Append('(').Append(callParamsStr).Append(')');
        }

        if (ObjectType is DbObjectType.VirtualView or DbObjectType.CustomSqlStatement)
        {
            sb.Append(" AS __storm_virtual_view");
        }

        AppendTableHints(queryParameters.TableHints, sb);

        var paramIndex = 1;
        GenerateSqlWhere(command, queryParameters, null, ref paramIndex, sb);
        GenerateTopSkipAndOrderBy(queryParameters, true, true, true, sb);

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);

        var commandBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow;
        if (queryParameters.CloseConnection)
            commandBehavior |= CommandBehavior.CloseConnection;

        return (commandBehavior, propertyName);
    }

    private (CommandBehavior commandBehavior, string propertyName1, string propertyName2) GetColumnValues<T, TColumn1, TColumn2>(
        StormVirtualDbCommand command,
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        SelectQueryParameters<T> queryParameters) where T : IDataBindable
    {
        var propertyName1 = columnSelector1.GetPropertyNameFromExpression();
        var propertyName2 = columnSelector2.GetPropertyNameFromExpression();

        var columnDef1 = Array.Find(ColumnDefs, x => string.Equals(x.PropertyName, propertyName1, StringComparison.Ordinal))
                         ?? throw new StormException($"Column '{propertyName1}' not found in the column definitions.");
        var columnDef2 = Array.Find(ColumnDefs, x => string.Equals(x.PropertyName, propertyName2, StringComparison.Ordinal))
                         ?? throw new StormException($"Column '{propertyName2}' not found in the column definitions.");

        var callParamsStr = command.GenerateCallParameters(queryParameters.CallParameters, ObjectType == DbObjectType.CustomSqlStatement ? CallParameterType.CustomSqlStatement : CallParameterType.Function);

        var sb = StormManager.GetStringBuilderFromPool();

        sb.Append("SELECT ").Append(columnDef1.ColumnName).Append(',').AppendLine(columnDef2.ColumnName);
        sb.Append("FROM ").Append(QuotedObjectFullName);

        if (callParamsStr is not null)
        {
            sb.Append('(').Append(callParamsStr).Append(')');
        }

        if (ObjectType is DbObjectType.VirtualView or DbObjectType.CustomSqlStatement)
        {
            sb.Append(" AS __storm_virtual_view");
        }

        AppendTableHints(queryParameters.TableHints, sb);

        var paramIndex = 1;
        GenerateSqlWhere(command, queryParameters, null, ref paramIndex, sb);
        GenerateTopSkipAndOrderBy(queryParameters, true, true, true, sb);

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);

        var commandBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow;
        if (queryParameters.CloseConnection)
            commandBehavior |= CommandBehavior.CloseConnection;

        return (commandBehavior, propertyName1, propertyName2);
    }

    private (CommandBehavior commandBehavior, string propertyName1, string propertyName2, string propertyName3) GetColumnValues<T, TResult1, TResult2, TResult3>(
        StormVirtualDbCommand command,
        Expression<Func<T, TResult1>> columnSelector1,
        Expression<Func<T, TResult2>> columnSelector2,
        Expression<Func<T, TResult3>> columnSelector3,
        SelectQueryParameters<T> queryParameters) where T : IDataBindable
    {
        var propertyName1 = columnSelector1.GetPropertyNameFromExpression();
        var propertyName2 = columnSelector2.GetPropertyNameFromExpression();
        var propertyName3 = columnSelector3.GetPropertyNameFromExpression();

        var columnDef1 = Array.Find(ColumnDefs, x => string.Equals(x.PropertyName, propertyName1, StringComparison.Ordinal))
                         ?? throw new StormException($"Column '{propertyName1}' not found in the column definitions.");
        var columnDef2 = Array.Find(ColumnDefs, x => string.Equals(x.PropertyName, propertyName2, StringComparison.Ordinal))
                         ?? throw new StormException($"Column '{propertyName2}' not found in the column definitions.");
        var columnDef3 = Array.Find(ColumnDefs, x => string.Equals(x.PropertyName, propertyName3, StringComparison.Ordinal))
                         ?? throw new StormException($"Column '{propertyName3}' not found in the column definitions.");

        var callParamsStr = command.GenerateCallParameters(queryParameters.CallParameters, ObjectType == DbObjectType.CustomSqlStatement ? CallParameterType.CustomSqlStatement : CallParameterType.Function);

        var sb = StormManager.GetStringBuilderFromPool();

        sb.Append("SELECT ").Append(columnDef1.ColumnName).Append(',').Append(columnDef2.ColumnName).Append(',').AppendLine(columnDef3.ColumnName);
        sb.Append("FROM ").Append(QuotedObjectFullName);

        if (callParamsStr is not null)
        {
            sb.Append('(').Append(callParamsStr).Append(')');
        }

        if (ObjectType is DbObjectType.VirtualView or DbObjectType.CustomSqlStatement)
        {
            sb.Append(" AS __storm_virtual_view");
        }

        AppendTableHints(queryParameters.TableHints, sb);

        var paramIndex = 1;
        GenerateSqlWhere(command, queryParameters, null, ref paramIndex, sb);
        GenerateTopSkipAndOrderBy(queryParameters, true, true, true, sb);

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);

        var commandBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow;
        if (queryParameters.CloseConnection)
            commandBehavior |= CommandBehavior.CloseConnection;

        return (commandBehavior, propertyName1, propertyName2, propertyName3);
    }

    private (CommandBehavior commandBehavior, string propertyName) ListColumnValues<T, TColumn>(
        StormVirtualDbCommand command,
        Expression<Func<T, TColumn>> columnSelector,
        SelectQueryParameters<T> queryParameters) where T : IDataBindable
    {
        var propertyName = columnSelector.GetPropertyNameFromExpression();
        var columnDef = Array.Find(ColumnDefs,
                            x => string.Equals(x.PropertyName, propertyName, StringComparison.Ordinal))
                        ?? throw new StormException($"Column '{propertyName}' not found in the column definitions.");

        var callParamsStr = command.GenerateCallParameters(queryParameters.CallParameters, ObjectType == DbObjectType.CustomSqlStatement ? CallParameterType.CustomSqlStatement : CallParameterType.Function);

        var sb = StormManager.GetStringBuilderFromPool();

        sb.Append("SELECT ").AppendLine(columnDef.ColumnName);
        sb.Append("FROM ").Append(QuotedObjectFullName);

        if (callParamsStr is not null)
        {
            sb.Append('(').Append(callParamsStr).Append(')');
        }

        if (ObjectType is DbObjectType.VirtualView or DbObjectType.CustomSqlStatement)
        {
            sb.Append(" AS __storm_virtual_view");
        }

        var paramIndex = 1;
        GenerateSqlWhere(command, queryParameters, null, ref paramIndex, sb);
        GenerateTopSkipAndOrderBy(queryParameters, true, false, true, sb);

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);

        var commandBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;
        if (queryParameters.CloseConnection)
            commandBehavior |= CommandBehavior.CloseConnection;

        return (commandBehavior, propertyName);
    }

    private (CommandBehavior commandBehavior, string propertyName1, string propertyName2) ListColumnValues<T, TColumn1, TColumn2>(
        StormVirtualDbCommand command,
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        SelectQueryParameters<T> queryParameters) where T : IDataBindable
    {
        var propertyName1 = columnSelector1.GetPropertyNameFromExpression();
        var propertyName2 = columnSelector2.GetPropertyNameFromExpression();

        var columnDef1 = Array.Find(ColumnDefs, x => string.Equals(x.PropertyName, propertyName1, StringComparison.Ordinal))
                         ?? throw new StormException($"Column '{propertyName1}' not found in the column definitions.");
        var columnDef2 = Array.Find(ColumnDefs, x => string.Equals(x.PropertyName, propertyName2, StringComparison.Ordinal))
                         ?? throw new StormException($"Column '{propertyName2}' not found in the column definitions.");

        var callParamsStr = command.GenerateCallParameters(queryParameters.CallParameters, ObjectType == DbObjectType.CustomSqlStatement ? CallParameterType.CustomSqlStatement : CallParameterType.Function);

        var sb = StormManager.GetStringBuilderFromPool();

        sb.Append("SELECT ").Append(columnDef1.ColumnName).Append(',').AppendLine(columnDef2.ColumnName);
        sb.Append("FROM ").Append(QuotedObjectFullName);

        if (callParamsStr is not null)
        {
            sb.Append('(').Append(callParamsStr).Append(')');
        }

        if (ObjectType is DbObjectType.VirtualView or DbObjectType.CustomSqlStatement)
        {
            sb.Append(" AS __storm_virtual_view");
        }

        var paramIndex = 1;
        GenerateSqlWhere(command, queryParameters, null, ref paramIndex, sb);
        GenerateTopSkipAndOrderBy(queryParameters, true, false, true, sb);

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);

        var commandBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;
        if (queryParameters.CloseConnection)
            commandBehavior |= CommandBehavior.CloseConnection;

        return (commandBehavior, propertyName1, propertyName2);
    }

    private (CommandBehavior commandBehavior, string propertyName1, string propertyName2, string propertyName3) ListColumnValues<T, TColumn1, TColumn2, TColumn3>(
        StormVirtualDbCommand command,
        Expression<Func<T, TColumn1>> columnSelector1,
        Expression<Func<T, TColumn2>> columnSelector2,
        Expression<Func<T, TColumn3>> columnSelector3,
        SelectQueryParameters<T> queryParameters) where T : IDataBindable
    {
        var propertyName1 = columnSelector1.GetPropertyNameFromExpression();
        var propertyName2 = columnSelector2.GetPropertyNameFromExpression();
        var propertyName3 = columnSelector3.GetPropertyNameFromExpression();

        var columnDef1 = Array.Find(ColumnDefs, x => string.Equals(x.PropertyName, propertyName1, StringComparison.Ordinal))
                         ?? throw new StormException($"Column '{propertyName1}' not found in the column definitions.");
        var columnDef2 = Array.Find(ColumnDefs, x => string.Equals(x.PropertyName, propertyName2, StringComparison.Ordinal))
                         ?? throw new StormException($"Column '{propertyName2}' not found in the column definitions.");
        var columnDef3 = Array.Find(ColumnDefs, x => string.Equals(x.PropertyName, propertyName3, StringComparison.Ordinal))
                         ?? throw new StormException($"Column '{propertyName2}' not found in the column definitions.");

        var callParamsStr = command.GenerateCallParameters(queryParameters.CallParameters, ObjectType == DbObjectType.CustomSqlStatement ? CallParameterType.CustomSqlStatement : CallParameterType.Function);

        var sb = StormManager.GetStringBuilderFromPool();

        sb.Append("SELECT ").Append(columnDef1.ColumnName).Append(',').Append(columnDef2.ColumnName).Append(',').AppendLine(columnDef3.ColumnName);
        sb.Append("FROM ").Append(QuotedObjectFullName);

        if (callParamsStr is not null)
        {
            sb.Append('(').Append(callParamsStr).Append(')');
        }

        if (ObjectType is DbObjectType.VirtualView or DbObjectType.CustomSqlStatement)
        {
            sb.Append(" AS __storm_virtual_view");
        }

        var paramIndex = 1;
        GenerateSqlWhere(command, queryParameters, null, ref paramIndex, sb);
        GenerateTopSkipAndOrderBy(queryParameters, true, false, true, sb);

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);

        var commandBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;
        if (queryParameters.CloseConnection)
            commandBehavior |= CommandBehavior.CloseConnection;

        return (commandBehavior, propertyName1, propertyName2, propertyName3);
    }

    /// <summary>
    /// Generates a SELECT SQL statement based on the provided parameters.
    /// </summary>
    /// <param name="partialLoadFlags">Flags indicating which columns to partially load.</param>
    /// <param name="tableAlias">An optional alias for the table.</param>
    /// <param name="callParamsStr">Procedure/Function input/output parameters.</param>
    /// <param name="tableHints">Table hints for a query.</param>
    /// <param name="sb">The StringBuilder to which the generated SQL will be appended.</param>
    private void GenerateSelectSql(uint partialLoadFlags, string? tableAlias, string? callParamsStr, StormTableHints tableHints, StringBuilder sb)
    {
        if (callParamsStr is null && _queryCache.TryGetValue((partialLoadFlags, tableAlias), out var sql))
        {
            sb.Append(sql);
            AppendTableHints(tableHints, sb);
            return;
        }

        sb.Append("SELECT ");
        sb.AppendJoin(',', ColumnDefs.GetSelectableColumns(partialLoadFlags, tableAlias));
        sb.AppendLine();
        sb.Append("FROM ").Append(QuotedObjectFullName);

        if (callParamsStr is not null)
            sb.Append('(').Append(callParamsStr).Append(')');

        if (tableAlias is not null)
            sb.Append(" AS ").Append(tableAlias);

        if (callParamsStr is null)
            _queryCache.TryAdd((partialLoadFlags, tableAlias), sb.ToString());

        AppendTableHints(tableHints, sb);
    }

    /// <summary>
    /// Generates a SELECT statement for retrieving details of a column.
    /// </summary>
    /// <param name="column">The column definition.</param>
    /// <param name="pkSelect">The primary key select statement.</param>
    /// <param name="tableAlias">The table alias.</param>
    /// <param name="sb">The string builder to append the SQL statement to.</param>
    private void GenerateSelectDetailSql(StormColumnDef column, string pkSelect, string? tableAlias, StringBuilder sb)
    {
        Debug.Assert(column.DetailType is not null);

        var detCtrl = StormControllerCache.Get(column.DetailType, 0);

        sb.Append("SELECT ").Append(pkSelect).Append(',');
        sb.AppendJoin(',', detCtrl.ColumnDefs.GetSelectableColumns(uint.MaxValue, tableAlias));
        sb.AppendLine();
        sb.Append("FROM ").Append(QuotedSchemaName).Append('.').Append(column.QuotedDetailTableName);

        if (tableAlias is not null)
        {
            sb.Append(" AS ").Append(tableAlias);
        }
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a SQL query to count the number of records based on the provided WHERE clause and query parameters.
    /// </summary>
    /// <param name="command">The database command to execute.</param>
    /// <param name="queryParameters">The query parameters.</param>
    /// <param name="callParamsStr">Procedure/Function input/output parameters.</param>
    /// <param name="sb">The StringBuilder object to which the SQL query is appended.</param>
    /// <returns>
    /// The StringBuilder object containing the generated SQL query for counting the number of records.
    /// </returns>
    private void GenerateCountSql<T>(IVirtualStormDbCommand command, SelectQueryParameters<T> queryParameters, string? callParamsStr, StringBuilder sb) where T : IDataBindable
    {
        sb.Append("SELECT COUNT(*) FROM ").Append(QuotedObjectFullName);

        if (callParamsStr is not null)
        {
            sb.Append('(').Append(callParamsStr).Append(')');
        }

        if (ObjectType is DbObjectType.VirtualView or DbObjectType.CustomSqlStatement)
        {
            sb.Append(" AS __storm_virtual_view");
        }

        AppendTableHints(queryParameters.TableHints, sb);

        var paramIndex = 1;
        GenerateSqlWhere(command, queryParameters, null, ref paramIndex, sb);
        sb.AppendLine();
    }

    /// <summary>
    /// Generates a SQL query to check if a record exists based on the provided WHERE clause and query parameters.
    /// </summary>
    /// <param name="command">The database command to execute.</param>
    /// <param name="queryParameters">The query parameters.</param>
    /// <param name="callParamsStr">Procedure/Function input/output parameters.</param>
    /// <param name="sb">The StringBuilder object to which the SQL query is being appended.</param>
    /// <returns>
    /// The StringBuilder object containing the SQL query to check if a record exists.
    /// </returns>
    private void GenerateExistsSql<T>(IVirtualStormDbCommand command, SelectQueryParameters<T> queryParameters, string? callParamsStr, StringBuilder sb) where T : IDataBindable
    {
        sb.Append("IF EXISTS (SELECT * FROM ").Append(QuotedObjectFullName);

        if (callParamsStr is not null)
        {
            sb.Append('(').Append(callParamsStr).Append(')');
        }

        if (ObjectType is DbObjectType.VirtualView or DbObjectType.CustomSqlStatement)
        {
            sb.Append(" AS __storm_virtual_view");
        }

        AppendTableHints(queryParameters.TableHints, sb);

        var paramIndex = 1;
        GenerateSqlWhere(command, queryParameters, null, ref paramIndex, sb);
        sb.AppendLine(") SELECT 1 ELSE SELECT 0");
    }

    /// <summary>
    /// Executes a scalar query asynchronously and returns the result of type T.
    /// </summary>
    /// <typeparam name="T">The type of the result to be returned.</typeparam>
    /// <param name="command">The StormDbCommand object associated with the query.</param>
    /// <param name="sql">The SQL query to be executed.</param>
    /// <param name="queryParameters">The query parameters.</param>
    /// <param name="cancellationToken">The CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>
    /// The result of the scalar query execution of type T.
    /// </returns>
    private static async Task<T> InternalExecuteScalarQueryAsync<T>(StormDbCommand command, string sql, QueryParameters queryParameters, CancellationToken cancellationToken)
    {
        command.SetStormCommandBaseParameters(queryParameters.Context, sql, queryParameters);

        var connection = queryParameters.Context.GetConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return (T)await command.ExecuteScalarCommandAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates the FETCH (TOP), OFFSET (SKIP) and ORDER BY clauses for a SQL query based on the provided parameters.
    /// </summary>
    private void GenerateTopSkipAndOrderBy<T>(SelectQueryParameters<T> queryParameters, bool useColumnNames, bool getOnlyFirstRow, bool shouldBeOrdered, StringBuilder sb)
        where T : IDataBindable
    {
        var rows = getOnlyFirstRow ? 1 : queryParameters.TopRows;
        var skip = queryParameters.SkipRows ?? (rows.HasValue ? 0 : null);
        var orderBy = queryParameters.OrderByColumnIds;
        if (orderBy?.Length == 0)
            orderBy = null;

        if (!skip.HasValue && orderBy is null)
            return;

        sb.AppendLine();

        if (orderBy is not null) // orderBy length is guaranteed to be > 0. See previous if statement.
        {
            if (useColumnNames)
            {
                sb.Append("ORDER BY ").AppendJoinFast(orderBy, ',',
                    (builder, x) =>
                    {
                        if (x >= 0)
                            builder.Append(ColumnDefs[x - 1].ColumnName);
                        else
                            builder.Append(ColumnDefs[-x - 1].ColumnName).Append(" DESC");
                    });
            }
            else
            {
                sb.Append("ORDER BY ").AppendJoinFast(orderBy, ',',
                    static (builder, x) =>
                    {
                        if (x >= 0)
                            builder.Append(x.ToString(CultureInfo.InvariantCulture));
                        else
                            builder.Append((-x).ToString(CultureInfo.InvariantCulture)).Append(" DESC");
                    });
            }
        }
        else
        if (shouldBeOrdered)
        {
            // Order by Keys
            sb.Append("ORDER BY ").AppendJoinFast(KeyColumnDefs[0], ',', static (builder, x) => builder.Append(x.ColumnName));
        }

        if (skip.HasValue)
        {
            sb.AppendLine().Append("OFFSET ").Append(skip.Value).Append(" ROWS");
        }

        if (rows.HasValue)
        {
            sb.AppendLine().Append("FETCH NEXT ").Append(rows.Value).Append(" ROWS ONLY");
        }

        sb.AppendLine();
    }

    private (int startIndex, int len) GenerateSqlWhere<T>(IVirtualStormDbCommand command, IKeyAndWhereExpression<T> queryParameters, string? tableAlias, ref int paramIndex, StringBuilder sb)
        where T : IDataBindable
    {
        var startIndex = sb.Length;

        var keyValues = queryParameters.KeyValues;
        if (keyValues is not null)
        {
            var indexId = queryParameters.KeyId!.Value;
            Debug.Assert(keyValues.Length != 0);

            var keyColumns = KeyColumnDefs[indexId];
            var haveAlias = tableAlias is not null;

            for (var i = 0; i < keyColumns.Length; i++)
            {
                var column = keyColumns[i];
                if (i == 0)
                {
                    sb.AppendLine().Append("WHERE ");
                }
                else
                {
                    sb.Append(" AND ");
                }

                if (haveAlias)
                {
                    sb.Append(tableAlias).Append('.');
                }

                sb.Append(column.ColumnName).Append('=').Append(command.AddDbParameter(paramIndex++, column, keyValues[i]));
            }
        }

        var whereExpressions = queryParameters.WhereExpressions;
        var oDataFilter = queryParameters.ODataFilter;

        if (whereExpressions is null && oDataFilter is null)
            return (startIndex, sb.Length - startIndex);

        if (keyValues is not null)
        {
            sb.Append(" AND ");
        }
        {
            sb.AppendLine().Append("WHERE ");
        }

        if (whereExpressions is not null)
        {
            SqlStatementGenerator.GenerateWhereSql(command, whereExpressions, ColumnDefs, tableAlias, ref paramIndex, sb);
        }

        if (oDataFilter is not null)
        {
            if (whereExpressions is not null)
                sb.Append(" AND ");
            ODataFilterStatementGenerator.GenerateSql(command, oDataFilter, ColumnDefs, tableAlias, ref paramIndex, sb);
        }

        return (startIndex, sb.Length - startIndex);
    }

    private (CommandBehavior commandBehavior, uint partialLoadFlags, bool shouldLoadDetails) GenerateSelectSql<T>(
         StormVirtualDbCommand command,
         SelectQueryParameters<T> queryParameters,
         bool getOnlyFirstRow)
         where T : IDataBindable
    {
        var partialLoadFlags = queryParameters.PartialLoadFlags;
        (partialLoadFlags, var shouldLoadDetails) = GetPartialLoadingData(partialLoadFlags, queryParameters);

        var callParamsStr = command.GenerateCallParameters(queryParameters.CallParameters, ObjectType == DbObjectType.CustomSqlStatement ? CallParameterType.CustomSqlStatement : CallParameterType.Function);

        var sb = StormManager.GetStringBuilderFromPool();

        var paramIndex = 1;
        GenerateSelectSql(partialLoadFlags, "A", callParamsStr, queryParameters.TableHints, sb);

        var whereLocation = GenerateSqlWhere(command, queryParameters, "A", ref paramIndex, sb);
        GenerateTopSkipAndOrderBy(queryParameters, false, getOnlyFirstRow, true, sb);

        if (shouldLoadDetails)
        {
            GenerateSelectDetailsSql(whereLocation, partialLoadFlags, sb);
        }

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);

        var commandBehavior = CommandBehavior.SequentialAccess;
        if (!shouldLoadDetails)
        {
            commandBehavior |= CommandBehavior.SingleResult;
            if (getOnlyFirstRow)
                commandBehavior |= CommandBehavior.SingleRow;
        }

        if (queryParameters.CloseConnection)
            commandBehavior |= CommandBehavior.CloseConnection;

        return (commandBehavior, partialLoadFlags, shouldLoadDetails);
    }

    /// <summary>
    /// Generates and executes an asynchronous SQL query to retrieve data from a database.
    /// </summary>
    /// <typeparam name="T">The type of data to retrieve.</typeparam>
    /// <param name="command">The database command.</param>
    /// <param name="queryParameters">The Query parameters.</param>
    /// <param name="partialLoadFlags">The flags indicating which columns to partially load.</param>
    /// <param name="shouldLoadDetails">A flag indicating whether to load details for the retrieved data.</param>
    /// <param name="getOnlyFirstRow">Get only first row</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A data reader containing the retrieved data.</returns>
    private async Task<StormDbDataReader> GenerateSqlAndExecuteReaderAsync<T>(
        StormDbCommand command,
        SelectQueryParameters<T> queryParameters,
        uint partialLoadFlags,
        bool shouldLoadDetails,
        bool getOnlyFirstRow,
        CancellationToken cancellationToken)
        where T : IDataBindable
    {
        var callParamsStr = command.GenerateCallParameters(queryParameters.CallParameters, ObjectType == DbObjectType.CustomSqlStatement ? CallParameterType.CustomSqlStatement : CallParameterType.Function);

        var sb = StormManager.GetStringBuilderFromPool();

        var paramIndex = 1;
        GenerateSelectSql(partialLoadFlags, "A", callParamsStr, queryParameters.TableHints, sb);

        var vCommand = new StormVirtualDbCommand(command);
        var whereLocation = GenerateSqlWhere(vCommand, queryParameters, "A", ref paramIndex, sb);
        GenerateTopSkipAndOrderBy(queryParameters, false, getOnlyFirstRow, true, sb);

        if (shouldLoadDetails)
        {
            GenerateSelectDetailsSql(whereLocation, partialLoadFlags, sb);
        }

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);

        var commandBehavior = CommandBehavior.SequentialAccess;
        if (!shouldLoadDetails)
        {
            commandBehavior |= CommandBehavior.SingleResult;
            if (getOnlyFirstRow)
                commandBehavior |= CommandBehavior.SingleRow;
        }

        if (queryParameters.CloseConnection)
            commandBehavior |= CommandBehavior.CloseConnection;

        var connection = queryParameters.Context.GetConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await command.ExecuteCommandReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
    }

    private void GenerateSelectDetailsSql((int startIndex, int length) whereLocation, uint partialLoadFlags, StringBuilder sb)
    {
        var keyColumns = KeyColumnDefs[0];

        var joinSb = StormManager.GetStringBuilderFromPool();
        joinSb.AppendJoinFast(keyColumns, ',', static (builder, x) => builder.Append("D.").Append(x.ColumnName));
        var pkSelect = joinSb.ToString();
        joinSb.Clear();

        var join = joinSb
            .Append("  INNER JOIN ").Append(QuotedObjectFullName).Append(" AS A ON ")
            .AppendJoinFast(keyColumns, " AND ", static (builder, x) => builder.Append("D.").Append(x.ColumnName).Append("=A.").Append(x.ColumnName))
            .ToStringAndReturnToPool();

        foreach (var column in ColumnDefs.GetDetailColumns(partialLoadFlags))
        {
            Debug.Assert(column.DetailType is not null);

            if (column.DetailType.IsPrimitiveEnumOrDomainValue())
            {
                sb.Append("SELECT ").Append(pkSelect).Append(",D.").AppendLine(column.ColumnName);
                sb.Append("FROM ").Append(QuotedSchemaName).Append('.').Append(column.QuotedDetailTableName).AppendLine(" AS D");
            }
            else
            {
                sb.AppendLine();
                GenerateSelectDetailSql(column, pkSelect, "D", sb);
            }

            sb.Append(join);

            if (whereLocation.length > 0)
            {
                sb.Append(sb, whereLocation.startIndex, whereLocation.length);
            }

            sb.AppendLine();
        }
    }

    #endregion Private methods
}
