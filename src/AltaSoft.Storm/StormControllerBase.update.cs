using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Helpers;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm;

/// <summary>
/// Abstract base class for ORM (Object-Relational Mapping) controllers, providing common functionality for database operations.
/// </summary>
public abstract partial class StormControllerBase
{
    internal async Task<int> UpdateAsync<T>(
           List<(StormColumnDef column, object? value)> setInstructions,
           ModifyQueryParameters<T> queryParameters,
           CancellationToken cancellationToken = default)
           where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);

            Update(vCommand, setInstructions, queryParameters);

            var connection = queryParameters.Context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            return await command.ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<int> UpdateAsync<T>(
        T value,
        bool checkConcurrency,
        ModifyQueryParameters<T> queryParameters,
        CancellationToken cancellationToken = default)
        where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);

            Update(vCommand, value, checkConcurrency, queryParameters);
            if (vCommand.CommandText.Length == 0) // no changes, return
                return 0;

            var connection = queryParameters.Context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            return await command.ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<int> UpdateAsync<T>(
        IEnumerable<T> valueList,
        bool checkConcurrency,
        ModifyQueryParameters<T> queryParameters,
        CancellationToken cancellationToken = default)
        where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);

            Update(vCommand, valueList, checkConcurrency, queryParameters);
            if (vCommand.CommandText.Length == 0) // no changes, return
                return 0;

            var connection = queryParameters.Context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            return await command.ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    #region Updates

    internal void Update<T>(
        IVirtualStormDbCommand command,
        List<(StormColumnDef column, object? value)> setInstructions,
        ModifyQueryParameters<T> queryParameters)
        where T : IDataBindable
    {
        var paramIndex = 1;
        var sb = StormManager.GetStringBuilderFromPool();

        sb.Append("UPDATE ").AppendLine(QuotedObjectFullName);
        sb.Append("SET ");

        sb.AppendJoinFast(setInstructions, ',', (builder, x) =>
        {
            var column = x.column;
            // ReSharper disable once AccessToModifiedClosure
            builder.Append(column.ColumnName).Append('=');

            if (x.value is LambdaExpression lambda && lambda.Parameters.Count == 1 && lambda.Parameters[0].Type == typeof(T))
            {
                // Wrap original expression into Expression<Func<T, object?>>
                var param = lambda.Parameters[0];
                var body = lambda.Body;

                // Add boxing if necessary
                if (body.Type.IsValueType)
                    body = Expression.Convert(body, typeof(object));

                var castedLambda = Expression.Lambda<Func<T, object?>>(body, param);
                SqlStatementGenerator.GenerateValueSql(command, castedLambda, ColumnDefs, null, ref paramIndex, sb);
            }
            else
            {
                builder.Append(command.AddDbParameter(paramIndex++, column, column.GetValueForDbParameter(x.value, x.column.PropertySerializationType)));
            }
        });

        GenerateSqlWhere(command, queryParameters, null, ref paramIndex, sb);
        sb.AppendLine();

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);
    }

    internal void Update<T>(
        IVirtualStormDbCommand command,
        T value,
        bool checkConcurrency,
        ModifyQueryParameters<T> queryParameters)
        where T : IDataBindable
    {
        var sb = StormManager.GetStringBuilderFromPool();
        sb.AppendLine("SET XACT_ABORT ON;");
        sb.AppendLine("BEGIN TRAN;");
        if (checkConcurrency)
            sb.AppendLine("DECLARE @__storm_concurrency_error__ bit = 0;");

        var savedPosition = sb.Length;

        var paramIndex = 1;
        GenerateUpdateSql(command, value, checkConcurrency, ref paramIndex, sb);

        if (sb.Length == savedPosition) // no changes, return
        {
            StormManager.ReturnStringBuilderToPool(sb);
            return;
        }
        sb.AppendLine("COMMIT TRAN;");

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);
    }

    internal void Update<T>(
        IVirtualStormDbCommand command,
        IEnumerable<T> valueList,
        bool checkConcurrency,
        ModifyQueryParameters<T> queryParameters)
        where T : IDataBindable
    {
        var sb = StormManager.GetStringBuilderFromPool();
        sb.AppendLine("SET XACT_ABORT ON;");
        sb.AppendLine("BEGIN TRAN;");
        if (checkConcurrency)
            sb.AppendLine("DECLARE @__storm_concurrency_error__ bit = 0;");

        var savedPosition = sb.Length;
        var paramIndex = 1;
        foreach (var value in valueList)
        {
            GenerateUpdateSql(command, value, checkConcurrency, ref paramIndex, sb);
        }

        if (sb.Length == savedPosition) // no changes, return
        {
            StormManager.ReturnStringBuilderToPool(sb);
            return;
        }
        sb.AppendLine("COMMIT TRAN;");

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);
    }

    private (List<string> whereStatements, List<string> pkColumnNames, List<string> pkParamNames)
        GenerateUpdateRowSql(IVirtualStormDbCommand command, IDataBindable value, IReadOnlyList<(StormColumnDef column, object? value)> columnValues,
            bool checkConcurrency, bool useRowCount, ref int paramIndex, string? indent, StringBuilder sb)
    {
        var pkInformation = GetPkInformation(command, value, ref paramIndex);
        if (columnValues.Count == 0) // no changes, return
            return pkInformation;

        var pid = paramIndex;

        sb.Append(indent).AppendLine("SET NOCOUNT OFF;");
        sb.Append(indent).Append("UPDATE ").AppendLine(QuotedObjectFullName);
        sb.Append(indent).Append("SET ");

        // ReSharper disable once SuspiciousTypeConversion.Global
        if (checkConcurrency && value is IConcurrencyCheck concurrencyCheckValue)
        {
            sb.Append("@__storm_concurrency_error__ = CASE WHEN ");
            sb.AppendJoinFast(concurrencyCheckValue.__ConcurrencyColumnValues(), " OR ", static x => (x.column.Flags & StormColumnFlags.ConcurrencyCheck) != StormColumnFlags.None,
                (builder, x) =>
                {
                    var parameterName = command.AddDbParameter(pid++, x.column, x.value);
                    builder.Append(x.column.ColumnName).Append("<>").Append(parameterName);
                });
            sb.Append(" THEN 1 ELSE 0 END, ");

            //var concurrencyWhereStatements = concurrencyCheckValue.__ConcurrencyColumnValues().FilterAndSelectList(
            //    static x => (x.column.Flags & StormColumnFlags.ConcurrencyCheck) != StormColumnFlags.None,
            //    x =>
            //    {
            //        var parameterName = AddDbParameter(command, pid++, x.column, x.value);
            //        return x.column.ColumnName + "<>" + parameterName;
            //    });
            //sb.AppendJoinFast(" AND ", concurrencyWhereStatements).AppendLine();
        }

        sb.AppendJoinFast(columnValues, ',', (builder, x) =>
        {
            var column = x.column;
            // ReSharper disable once AccessToModifiedClosure
            builder.Append(column.ColumnName).Append('=').Append(command.AddDbParameter(pid++, column, column.GetValueForDbParameter(x.value, x.column.PropertySerializationType)));
        });

        sb.AppendLine();
        sb.Append(indent).Append("WHERE ").AppendJoinFast(" AND ", pkInformation.whereStatements).AppendLine();

        if (useRowCount)
        {
            sb.Append(indent).AppendLine("SET @__storm_rows_affected__ = @@ROWCOUNT;");
        }

        if (checkConcurrency)
        {
            sb.Append(indent).Append("IF @__storm_concurrency_error__<>0 BEGIN ROLLBACK; THROW 900001, 'Concurrency error', 4; RETURN; END;");
        }

        sb.AppendLine();

        paramIndex = pid;
        return pkInformation;
    }

    /// <summary>
    /// Generates an SQL UPDATE command for a given value and appends it to a StringBuilder.
    /// </summary>
    /// <param name="command">The database command object to which parameters will be added.</param>
    /// <param name="value">The value implementing ITrackingObject from which to derive the values for updating.</param>
    /// <param name="checkConcurrency">Whether to check concurrency columns.</param>
    /// <param name="paramIndex">The starting index for parameterization in the SQL command.</param>
    /// <param name="sb">The StringBuilder to which the generated SQL will be appended.</param>
    /// <returns>The StringBuilder with the appended SQL UPDATE command.</returns>
    private void GenerateUpdateSql(IVirtualStormDbCommand command, IDataBindable value, bool checkConcurrency, ref int paramIndex, StringBuilder sb)
    {
        value.BeforeSave(SaveAction.Update);

        var columnValues = value.__GetColumnValues();
        columnValues = GetChangedColumnValues(value, columnValues);
        if (columnValues.Length == 0) // no changes, return
            return;

        var (masterColumns, detailColumns) = columnValues.GetMaterDetailColumnsForUpdate();

        var (masterPkWhereStatements, masterPkColumnNames, masterPkParamNames) = GenerateUpdateRowSql(command, value, masterColumns, checkConcurrency, false, ref paramIndex, null, sb);

        var pid = paramIndex;

        sb.AppendLine("SET NOCOUNT ON;");

        if (detailColumns is not null)
        {
            foreach (var (column, detailList) in detailColumns)
            {
                StormControllerBase? detCtrl;

                switch (detailList)
                {
                    case null: // Value is null, so delete all details
                        sb.Append("DELETE FROM ").Append(QuotedSchemaName).Append('.').AppendLine(column.QuotedDetailTableName);
                        sb.Append("WHERE ").AppendJoinFast(" AND ", masterPkWhereStatements).AppendLine();
                        continue;

                    case IEntityTrackingList entityList:
                        Debug.Assert(column.DetailType is not null);
                        Debug.Assert(column.QuotedDetailTableName is not null);

                        detCtrl = null;

                        foreach (var detailValue in entityList.GetDeletedEntities())
                        {
                            detCtrl ??= StormControllerCache.Get(detailValue.GetType(), 0);
                            detCtrl.GenerateDeleteDetailSql(command, column.QuotedDetailTableName, detailValue, masterPkWhereStatements, ref pid, sb);
                        }

                        foreach (var detailValue in entityList.GetUpdatedEntities())
                        {
                            detCtrl ??= StormControllerCache.Get(detailValue.GetType(), 0);
                            detCtrl.GenerateUpdateDetailSql(command, column.QuotedDetailTableName, detailValue, masterPkWhereStatements, ref pid, sb);
                        }

                        foreach (var detailValue in entityList.GetInsertedEntities())
                        {
                            detCtrl ??= StormControllerCache.Get(detailValue.GetType(), 0);
                            detCtrl.GenerateInsertDetailSql(command, column.QuotedDetailTableName, detailValue, masterPkColumnNames, masterPkParamNames, ref pid, sb);
                        }
                        break;

                    case ITrackingList<IDataBindable> trackingList:
                        Debug.Assert(column.DetailType is not null);
                        Debug.Assert(column.QuotedDetailTableName is not null);

                        detCtrl = null;

                        foreach (var detailValue in trackingList.GetDeletedObjects().Cast<IDataBindable>())
                        {
                            detCtrl ??= StormControllerCache.Get(detailValue.GetType(), 0);
                            detCtrl.GenerateDeleteDetailSql(command, column.QuotedDetailTableName, detailValue, masterPkWhereStatements, ref pid, sb);
                        }

                        foreach (var detailValue in trackingList.GetUpdatedObjects().Cast<IDataBindable>())
                        {
                            detCtrl ??= StormControllerCache.Get(detailValue.GetType(), 0);
                            detCtrl.GenerateUpdateDetailSql(command, column.QuotedDetailTableName, detailValue, masterPkWhereStatements, ref pid, sb);
                        }

                        foreach (var detailValue in trackingList.GetInsertedObjects().Cast<IDataBindable>())
                        {
                            detCtrl ??= StormControllerCache.Get(detailValue.GetType(), 0);

                            detCtrl.GenerateInsertDetailSql(command, column.QuotedDetailTableName, detailValue, masterPkColumnNames, masterPkParamNames, ref pid, sb);
                        }
                        break;

                    case ITrackingList trackingList:
                        Debug.Assert(column.DetailType is not null);
                        Debug.Assert(column.QuotedDetailTableName is not null);

                        foreach (var detValue in trackingList.GetDeletedObjects())
                        {
                            var parameterName = command.AddDbParameter(pid++, column, detValue);

                            sb.Append("DELETE FROM ").Append(QuotedSchemaName).Append('.').AppendLine(column.QuotedDetailTableName);
                            sb.Append("WHERE ").AppendJoinFast(" AND ", masterPkWhereStatements, [column.ColumnName + '=' + parameterName]).AppendLine();
                        }

                        foreach (var detValue in trackingList.GetInsertedObjects())
                        {
                            var parameterName = command.AddDbParameter(pid++, column, detValue);

                            sb.Append("INSERT INTO ").Append(QuotedSchemaName).Append('.').Append(column.QuotedDetailTableName).Append(" (").AppendJoinFast(',', masterPkColumnNames).Append(',').Append(column.ColumnName).Append(')').AppendLine();
                            sb.Append("VALUES (").AppendJoinFast(',', masterPkParamNames).Append(',').Append(parameterName).AppendLine(")");
                        }
                        break;

                    case IEnumerable<IDataBindable> list:
                        Debug.Assert(column.DetailType is not null);
                        Debug.Assert(column.QuotedDetailTableName is not null);

                        detCtrl = StormControllerCache.Get(column.DetailType, 0);

                        // We do not have change tracking for detail tables, so we delete all and insert all
                        sb.Append("DELETE FROM ").Append(QuotedSchemaName).Append('.').AppendLine(column.QuotedDetailTableName);
                        sb.Append("WHERE ").AppendJoinFast(" AND ", masterPkWhereStatements).AppendLine();

                        foreach (var detailValue in list)
                        {
                            detCtrl.GenerateInsertDetailSql(command, column.QuotedDetailTableName, detailValue, masterPkColumnNames, masterPkParamNames, ref pid, sb);
                        }

                        break;

                    case IEnumerable list:
                        Debug.Assert(column.QuotedDetailTableName is not null);

                        // We do not have change tracking for detail tables, so we delete all and insert all
                        sb.Append("DELETE FROM ").Append(QuotedSchemaName).Append('.').AppendLine(column.QuotedDetailTableName);
                        sb.Append("WHERE ").AppendJoinFast(" AND ", masterPkWhereStatements).AppendLine();

                        foreach (var detValue in list)
                        {
                            var parameterName = command.AddDbParameter(pid++, column, detValue);

                            sb.Append("INSERT INTO ").Append(QuotedSchemaName).Append('.').Append(column.QuotedDetailTableName).Append(" (").AppendJoinFast(',', masterPkColumnNames).Append(',').Append(column.ColumnName).Append(')').AppendLine();
                            sb.Append("VALUES (").AppendJoinFast(',', masterPkParamNames).Append(',').Append(parameterName).AppendLine(")");
                        }

                        break;
                }
            }
        }

        paramIndex = pid;
    }

    private void GenerateUpdateDetailSql(IVirtualStormDbCommand command, string detailTableName, IDataBindable detailValue, List<string> masterPkWhereStatements, ref int paramIndex, StringBuilder sb)
    {
        detailValue.BeforeSave(SaveAction.Update);

        var pid = paramIndex;

        var (whereStatements, _, _) = GetPkInformation(command, detailValue, ref pid);

        var columnValues = GetChangedColumnValues(detailValue, detailValue.__GetColumnValues());
        if (columnValues.Length == 0) // no changes, return
            return;

        sb.Append("UPDATE ").Append(QuotedSchemaName).Append('.').AppendLine(detailTableName);
        sb.Append("SET ");

        sb.AppendJoinFast(columnValues, ',',
            static x => x.column.CanUpdateColumn(), (builder, x) =>
                    {
                        var column = x.column;
                        // ReSharper disable once AccessToModifiedClosure
                        builder.Append(column.ColumnName).Append('=').Append(command.AddDbParameter(pid++, column, column.GetValueForDbParameter(x.value, x.column.PropertySerializationType)));
                    });
        sb.AppendLine();
        sb.Append("WHERE ").AppendJoinFast(" AND ", masterPkWhereStatements, whereStatements);
        sb.AppendLine();

        paramIndex = pid;
    }

    /// <summary>
    /// Retrieves the column values that have been changed in the given tracking object.
    /// </summary>
    /// <param name="value">The tracking object.</param>
    /// <param name="columnValues">Column values</param>
    /// <returns>An array of tuples containing the changed column definition and its corresponding value.</returns>
    private static (StormColumnDef column, object? value)[] GetChangedColumnValues(IDataBindable value, (StormColumnDef column, object? value)[] columnValues)
    {
        if (value is not ITrackingObject trackingValue)
            return columnValues;

        if (!trackingValue.IsChangeTrackingActive())
            return columnValues;

        var changed = trackingValue.__GetChangedPropertyNames();

        // Filter the columnValues array to include only those where the column name is in the changed list
        return columnValues.FilterAndSelectList(
            x => changed.Contains(x.column.PropertyName),
            static x => x).ToArray();
    }

    #endregion Updates
}
