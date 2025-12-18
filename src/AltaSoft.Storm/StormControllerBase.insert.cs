using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm;

/// <summary>
/// Abstract base class for ORM (Object-Relational Mapping) controllers, providing common functionality for database operations.
/// </summary>
public abstract partial class StormControllerBase
{
    internal async Task<int> InsertAsync<T>(
        T value,
        ModifyQueryParameters<T> queryParameters,
        CancellationToken cancellationToken = default)
        where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);

            Insert(vCommand, value, queryParameters);

            var connection = queryParameters.Context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var autoIncColumn = __GetAutoIncColumn();
            if (autoIncColumn is null)
                return await command.ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);

            var reader = await command.ExecuteCommandReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    return 0;

                value.__SetAutoIncValue(reader, 0);
                return 1;
            }
        }
    }

    internal async Task<int> InsertAsync<T>(
        IEnumerable<T> values,
        ModifyQueryParameters<T> queryParameters,
        CancellationToken cancellationToken = default)
        where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);

            var (autoIncColumn, valueList) = Insert(vCommand, values, queryParameters);
            if (vCommand.CommandText.Length == 0) // no changes, return
                return 0;

            var connection = queryParameters.Context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            if (autoIncColumn is null)
                return await command.ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);

            var count = 0;
            var reader = await command.ExecuteCommandReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (count >= valueList.Count)
                        throw new StormException("More than expected rows returned from the database.");
                    valueList[count++].__SetAutoIncValue(reader, 0);
                }
            }
            return count;
        }
    }

    #region Inserts

    internal void Insert<T>(
        IVirtualStormDbCommand command,
        T value,
        ModifyQueryParameters<T> queryParameters)
        where T : IDataBindable
    {
        var paramIndex = 1;
        var sb = StormManager.GetStringBuilderFromPool();

        GenerateInsertSql(command, value, ref paramIndex, -1, sb);

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);
    }

    internal (StormColumnDef? autoIncColumn, IList<T> valueList) Insert<T>(
        IVirtualStormDbCommand command,
        IEnumerable<T> values,
        ModifyQueryParameters<T> queryParameters)
        where T : IDataBindable
    {
        var paramIndex = 1;
        var autoIncColumn = __GetAutoIncColumn();
        var valueList = values.AsIList();

        var sb = StormManager.GetStringBuilderFromPool();

        if (autoIncColumn is not null)
            sb.Append("DECLARE @__storm_id_values TABLE ([Id] ").Append(StormManager.ToSqlDbType(autoIncColumn.DbType, 0, 0, 0)).AppendLine(" NOT NULL);");

        foreach (var value in valueList)
        {
            GenerateInsertSql(command, value, ref paramIndex, -2, sb);
        }

        if (sb.Length == 0)
        {
            StormManager.ReturnStringBuilderToPool(sb);
            return (autoIncColumn, valueList);
        }

        if (autoIncColumn is not null)
        {
            sb.AppendLine("SELECT [Id] FROM @__storm_id_values;");
        }

        if (!queryParameters.Context.IsInUnitOfWork)
        {
            sb.WrapIntoBeginTranCommit();
        }

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);
        return (autoIncColumn, valueList);
    }

    /// <summary>
    /// Generates an SQL INSERT command for a given value and appends it to a StringBuilder.
    /// </summary>
    /// <param name="command">The database command object to which parameters will be added.</param>
    /// <param name="value">The value implementing IDataBindable from which to derive the values for insertion.</param>
    /// <param name="paramIndex">The starting index for parameterization in the SQL command.</param>
    /// <param name="index">Index of row in multirow merge or insert, -1 means no temp table</param>
    /// <param name="sb">The StringBuilder to which the generated SQL will be appended.</param>
    /// <returns>The StringBuilder with the appended SQL INSERT command.</returns>
    private void GenerateInsertSql(IVirtualStormDbCommand command, IDataBindable value, ref int paramIndex, int index, StringBuilder sb)
    {
        value.BeforeSave(SaveAction.Insert);

        var (masterValues, detailValues) = value.__GetColumnValues().GetMaterAndDetailColumnsForInsert();

        var (masterPkColumnNames, masterPkParamNames) = GenerateInsertOneRowSql(command, masterValues, true, ref paramIndex, null, index, sb);

        var pid = paramIndex;

        if (detailValues is not null && masterPkColumnNames is not null && masterPkParamNames is not null)
        {
            var noCountAdded = false;

            foreach (var (column, detailList) in detailValues)
            {
                switch (detailList)
                {
                    case null:
                        continue;

                    case IEnumerable<IDataBindable> dataBindableList:
                        Debug.Assert(column.DetailType is not null);
                        Debug.Assert(column.QuotedDetailTableName is not null);

                        var detCtrl = StormControllerCache.Get(column.DetailType, 0);

                        foreach (var detailValue in dataBindableList)
                        {
                            EnsureNoCountOn();
                            detCtrl.GenerateInsertDetailSql(command, column.QuotedDetailTableName, detailValue, masterPkColumnNames, masterPkParamNames, ref pid, sb);
                        }

                        break;

                    case IEnumerable list:
                        Debug.Assert(column.QuotedDetailTableName is not null);

                        foreach (var detValue in list)
                        {
                            var parameterName = command.AddDbParameter(pid++, column, detValue); EnsureNoCountOn();
                            sb.Append("INSERT INTO ").Append(QuotedSchemaName).Append('.').Append(column.QuotedDetailTableName).Append(" (").AppendJoinFast(',', masterPkColumnNames).Append(',').Append(column.ColumnName).Append(')').AppendLine();
                            sb.Append("VALUES (").AppendJoinFast(',', masterPkParamNames).Append(',').Append(parameterName).AppendLine(")");
                        }

                        break;
                }
            }

            void EnsureNoCountOn()
            {
                if (noCountAdded)
                    return;
                sb.AppendLine("SET NOCOUNT ON;");
                noCountAdded = true;
            }
        }
        paramIndex = pid;
    }

    private (List<string>? masterPkColumnNames, List<string>? masterPkParamNames)
        GenerateInsertOneRowSql(IVirtualStormDbCommand command, IReadOnlyList<(StormColumnDef column, object? value)> columnValues,
            bool returnPkInfo, ref int paramIndex, string? indent, int index, StringBuilder sb)
    {
        var pid = paramIndex;
        var autoIncColumn = __GetAutoIncColumn();

        List<string>? masterPkColumnNames = null;
        List<string>? masterPkParamNames = null;

        sb.Append(indent).AppendLine("SET NOCOUNT OFF;");
        sb.Append(indent).Append("INSERT INTO ").Append(QuotedObjectFullName).Append(" (");
        sb.AppendJoin(',', columnValues.Select(x =>
        {
            var column = x.column;
            if (returnPkInfo && (column.Flags & StormColumnFlags.Key) != StormColumnFlags.None)
            {
                masterPkColumnNames ??= new List<string>(2);
                masterPkColumnNames.Add(column.ColumnName);
            }
            return column.ColumnName;
        }));
        sb.Append(')').AppendLine();

        if (autoIncColumn is not null)
        {
            if (index == -1)
            {
                sb.Append(indent).Append("OUTPUT INSERTED.").AppendLine(autoIncColumn.ColumnName);
            }
            else
            if (index < 0)
            {
                sb.Append(indent).Append("OUTPUT INSERTED.").Append(autoIncColumn.ColumnName).AppendLine(" INTO @__storm_id_values");
            }
            else
            {
                sb.Append(indent).Append("OUTPUT ").Append(index.ToString(CultureInfo.InvariantCulture)).Append(", INSERTED.").Append(autoIncColumn.ColumnName);
                sb.AppendLine(" INTO @__storm_id_values");
            }
        }

        sb.Append(indent).Append("VALUES (");
        sb.AppendJoin(',', columnValues.Select(x =>
        {
            var column = x.column;
            // ReSharper disable once AccessToModifiedClosure
            var parameterName = command.AddDbParameter(pid++, column, column.GetValueForDbParameter(x.value, x.column.PropertySerializationType));
            if (returnPkInfo && (column.Flags & StormColumnFlags.Key) != StormColumnFlags.None)
            {
                masterPkParamNames ??= new List<string>(2);
                masterPkParamNames.Add(parameterName);
            }
            return parameterName;
        }));
        sb.Append(indent).AppendLine(")");

        paramIndex = pid;
        return (masterPkColumnNames, masterPkParamNames);
    }

    /// <summary>
    /// Generates an SQL INSERT command for a given value and appends it to a StringBuilder.
    /// </summary>
    /// <param name="command">The database command object to which parameters will be added.</param>
    /// <param name="value">The value implementing IDataBindable from which to derive the values for insertion.</param>
    /// <param name="paramIndex">The starting index for parameterization in the SQL command.</param>
    /// <param name="sb">The StringBuilder to which the generated SQL will be appended.</param>
    /// <returns>The StringBuilder with the appended SQL INSERT command.</returns>
    private void GenerateBatchInsertSql(StormDbBatchCommand command, IDataBindable value, ref int paramIndex, StringBuilder sb)
    {
        value.BeforeSave(SaveAction.Insert);

        var (masterValues, detailValues) = value.__GetColumnValues().GetMaterAndDetailColumnsForInsert();

        List<string>? masterPkColumnNames = null;
        List<string>? masterPkParamNames = null;
        var pid = paramIndex;

        sb.Append("INSERT INTO ").Append(QuotedObjectFullName).Append(" (");
        sb.AppendJoin(',', masterValues.Select(x =>
                {
                    var column = x.column;
                    if (detailValues is not null && (column.Flags & StormColumnFlags.Key) != StormColumnFlags.None)
                    {
                        masterPkColumnNames ??= new List<string>(2);
                        masterPkColumnNames.Add(column.ColumnName);
                    }
                    return column.ColumnName;
                }));
        sb.Append(')').AppendLine();

        sb.Append("VALUES (");
        sb.AppendJoin(',', masterValues.Select(x =>
                {
                    var column = x.column;
                    var parameterName = command.AddDbParameter(pid++, column, column.GetValueForDbParameter(x.value, x.column.PropertySerializationType));
                    if (detailValues is not null && (column.Flags & StormColumnFlags.Key) != StormColumnFlags.None)
                    {
                        masterPkParamNames ??= new List<string>(2);
                        masterPkParamNames.Add(parameterName);
                    }
                    return parameterName;
                }));
        sb.AppendLine(")");

        if (detailValues is not null && masterPkColumnNames is not null && masterPkParamNames is not null)
        {
            var noCountAdded = false;

            foreach (var (column, detailList) in detailValues)
            {
                switch (detailList)
                {
                    case null:
                        continue;

                    case IEnumerable<IDataBindable> dataBindableList:
                        Debug.Assert(column.DetailType is not null);
                        Debug.Assert(column.QuotedDetailTableName is not null);

                        var detCtrl = StormControllerCache.Get(column.DetailType, 0);

                        foreach (var detailValue in dataBindableList)
                        {
                            EnsureNoCountOn();
                            detCtrl.GenerateInsertBatchDetailSql(command, column.QuotedDetailTableName, detailValue, masterPkColumnNames, masterPkParamNames, ref pid, sb);
                        }

                        break;

                    case IEnumerable<string> stringList:
                        Debug.Assert(column.QuotedDetailTableName is not null);

                        foreach (var detValue in stringList)
                        {
                            var parameterName = command.AddDbParameter(pid++, column, detValue);

                            EnsureNoCountOn();
                            sb.Append("INSERT INTO ").Append(QuotedSchemaName).Append('.').Append(column.QuotedDetailTableName).Append(" (").AppendJoinFast(',', masterPkColumnNames).Append(',').Append(column.ColumnName).Append(')').AppendLine();
                            sb.Append("VALUES (").AppendJoinFast(',', masterPkParamNames).Append(',').Append(parameterName).AppendLine(")");
                        }

                        break;
                }
            }

            void EnsureNoCountOn()
            {
                if (noCountAdded)
                    return;
                sb.AppendLine("SET NOCOUNT ON;");
                noCountAdded = true;
            }
        }
        paramIndex = pid;
    }

    /// <summary>
    /// Generates an SQL INSERT command for a detail table associated with the main value.
    /// </summary>
    private void GenerateInsertDetailSql(IVirtualStormDbCommand command, string detailTableName,
        IDataBindable detailValue, List<string> masterPkColumnNames, List<string> masterPkParamNames, ref int paramIndex, StringBuilder sb)
    {
        detailValue.BeforeSave(SaveAction.Insert);

        var pid = paramIndex;
        var detailColumnValues = detailValue.__GetColumnValues().GetColumnsForInsert();

        sb.Append("INSERT INTO ").Append(QuotedSchemaName).Append('.').Append(detailTableName).Append(" (").AppendJoinFast(',', masterPkColumnNames).Append(',');

        sb.AppendJoin(',', detailColumnValues.Select(x => x.column.ColumnName));
        sb.Append(')').AppendLine();

        sb.Append("VALUES (").AppendJoinFast(',', masterPkParamNames).Append(',');
        sb.AppendJoin(',', detailColumnValues.Select(x => command.AddDbParameter(pid++, x.column, x.column.GetValueForDbParameter(x.value, x.column.PropertySerializationType))));
        sb.AppendLine(")");

        paramIndex = pid;
    }

    /// <summary>
    /// Generates an SQL INSERT command for a detail table associated with the main value.
    /// </summary>
    /// <param name="command">The database command object to which parameters will be added.</param>
    /// <param name="quotedDetailTableName">The quoted name of the detail table for insertion.</param>
    /// <param name="detailValue">The detail value implementing IDataBindable from which to derive the values for insertion.</param>
    /// <param name="masterPkColumnNames">A comma-separated string of primary key column names in the detail table.</param>
    /// <param name="masterPkParamNames">A comma-separated string of primary key parameter names for the SQL command.</param>
    /// <param name="paramIndex">The starting index for parameterization in the SQL command.</param>
    /// <param name="sb">The StringBuilder to which the generated SQL will be appended.</param>
    /// <returns>The StringBuilder with the appended SQL INSERT command for the detail table.</returns>
    private void GenerateInsertBatchDetailSql(StormDbBatchCommand command, string quotedDetailTableName,
        IDataBindable detailValue, List<string> masterPkColumnNames, List<string> masterPkParamNames, ref int paramIndex, StringBuilder sb)
    {
        detailValue.BeforeSave(SaveAction.Insert);

        var pid = paramIndex;
        var detailColumnValues = detailValue.__GetColumnValues().GetColumnsForInsert();

        sb.Append("INSERT INTO ").Append(QuotedSchemaName).Append('.').Append(quotedDetailTableName).Append(" (").AppendJoinFast(',', masterPkColumnNames).Append(',');

        sb.AppendJoin(',', detailColumnValues.Select(x => x.column.ColumnName));
        sb.Append(')').AppendLine();

        sb.Append("VALUES (").AppendJoinFast(',', masterPkParamNames).Append(',');
        sb.AppendJoin(',', detailColumnValues.Select(x => command.AddDbParameter(pid++, x.column, x.column.GetValueForDbParameter(x.value, x.column.PropertySerializationType))));
        sb.AppendLine(")");

        paramIndex = pid;
    }

    #endregion Inserts
}
