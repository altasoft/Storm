using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm;

/// <summary>
/// Abstract base class for ORM (Object-Relational Mapping) controllers, providing common functionality for database operations.
/// </summary>
public abstract partial class StormControllerBase
{
    internal async Task<int> MergeAsync<T>(
         T value,
         bool checkConcurrency,
         bool updateThenInsert,
         ModifyQueryParameters<T> queryParameters,
         CancellationToken cancellationToken = default)
         where T : IDataBindable
    {
        if (!HasConcurrencyCheck)
            checkConcurrency = false;

        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);

            Merge(vCommand, value, checkConcurrency, updateThenInsert, queryParameters);

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
                    return 1;

                value.__SetAutoIncValue(reader, 0);
                return 1;
            }
        }
    }

    internal async Task<int> MergeAsync<T>(
        IEnumerable<T> values,
        bool checkConcurrency,
        bool updateThenInsert,
        ModifyQueryParameters<T> queryParameters,
        CancellationToken cancellationToken = default)
        where T : IDataBindable
    {
        if (!HasConcurrencyCheck)
            checkConcurrency = false;

        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);

            var (autoIncColumn, valueList) = Merge(vCommand, values, checkConcurrency, updateThenInsert, queryParameters);

            var connection = queryParameters.Context.GetConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            if (autoIncColumn is null)
                return await command.ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);

            var reader = await command.ExecuteCommandReaderAsync(CommandBehavior.SequentialAccess, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var index = reader.GetInt32(0);
                    valueList[index].__SetAutoIncValue(reader, 1);
                }
            }
            return valueList.Count;
        }
    }

    #region Merges

    internal void Merge<T>(
        IVirtualStormDbCommand command,
        T value,
        bool checkConcurrency,
        bool updateThenInsert,
        ModifyQueryParameters<T> queryParameters)
       where T : IDataBindable
    {
        if (!HasConcurrencyCheck)
            checkConcurrency = false;

        var sb = StormManager.GetStringBuilderFromPool();
        sb.AppendLine("SET XACT_ABORT ON;");
        sb.AppendLine("BEGIN TRAN;");
        if (checkConcurrency)
            sb.AppendLine("DECLARE @__storm_concurrency_error__ bit = 0;");
        if (updateThenInsert)
            sb.AppendLine("DECLARE @__storm_rows_affected__ int = 0;");

        var paramIndex = 1;
        GenerateMergeSql(command, value, checkConcurrency, updateThenInsert, ref paramIndex, -1, sb);
        sb.AppendLine("COMMIT TRAN;");

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);
    }

    internal (StormColumnDef? autoIncColumn, IList<T> valueList) Merge<T>(
        IVirtualStormDbCommand command,
        IEnumerable<T> values,
        bool checkConcurrency,
        bool updateThenInsert,
        ModifyQueryParameters<T> queryParameters)
        where T : IDataBindable
    {
        if (!HasConcurrencyCheck)
            checkConcurrency = false;

        var valueList = values.AsIList();
        var autoIncColumn = __GetAutoIncColumn();

        var sb = StormManager.GetStringBuilderFromPool();
        sb.AppendLine("SET XACT_ABORT ON;");
        if (autoIncColumn is not null)
            sb.Append("DECLARE @__storm_id_values TABLE ([Index] int NOT NULL, [Id] ").Append(StormManager.ToSqlDbType(autoIncColumn.DbType, 0, 0, 0)).AppendLine(" NOT NULL);");

        sb.AppendLine("BEGIN TRAN;");
        if (checkConcurrency)
            sb.AppendLine("DECLARE @__storm_concurrency_error__ bit = 0;");
        if (updateThenInsert)
            sb.AppendLine("DECLARE @__storm_rows_affected__ int = 0;");

        var paramIndex = 1;
        for (var index = 0; index < valueList.Count; index++)
        {
            GenerateMergeSql(command, valueList[index], checkConcurrency, updateThenInsert, ref paramIndex, index, sb);
        }

        sb.AppendLine("COMMIT TRAN;");

        if (autoIncColumn is not null)
        {
            sb.AppendLine("SELECT [Index],[Id] FROM @__storm_id_values;");
        }

        command.SetStormCommandBaseParameters(queryParameters.Context, sb.ToStringAndReturnToPool(), queryParameters);
        return (autoIncColumn, valueList);
    }

    /// <summary>
    /// Generates an SQL UPDATE/INSERT OR INSERT/UPDATE command for a given value and appends it to a StringBuilder.
    /// </summary>
    /// <param name="command">The database command object to which parameters will be added.</param>
    /// <param name="value">The value implementing ITrackingObject from which to derive the values for updating.</param>
    /// <param name="checkConcurrency">Whether to check concurrency columns.</param>
    /// <param name="updateThenInsert">First try update and then insert, or vise versa.</param>
    /// <param name="paramIndex">The starting index for parameterization in the SQL command.</param>
    /// <param name="index">Index of row in multirow merge, or -1</param>
    /// <param name="sb">The StringBuilder to which the generated SQL will be appended.</param>
    /// <returns>The StringBuilder with the appended SQL UPDATE/INSERT or INSERT/UPDATE command.</returns>
    private void GenerateMergeSql(IVirtualStormDbCommand command, IDataBindable value, bool checkConcurrency, bool updateThenInsert, ref int paramIndex, int index, StringBuilder sb)
    {
        value.BeforeSave(SaveAction.Merge);

        var columnsToUpdateValues = value.__GetColumnValues().GetColumnsForUpdate();
        var columnsToInsertValues = value.__GetColumnValues().GetColumnsForInsert();

        if (updateThenInsert)
        {
            GenerateUpdateRowSql(command, value, columnsToUpdateValues, checkConcurrency, true, ref paramIndex, null, sb);
            sb.AppendLine("IF @__storm_rows_affected__ = 0");
            sb.AppendLine("BEGIN");
            GenerateInsertOneRowSql(command, columnsToInsertValues, false, ref paramIndex, "  ", index, sb);
            sb.AppendLine("END");
        }
        else
        {
            sb.AppendLine("BEGIN TRY");
            GenerateInsertOneRowSql(command, columnsToInsertValues, false, ref paramIndex, null, index, sb);
            sb.AppendLine("END TRY");
            sb.AppendLine("BEGIN CATCH");
            sb.AppendLine("IF ERROR_NUMBER() NOT IN(2627, 2601) THROW;");
            sb.AppendLine("BEGIN");
            GenerateUpdateRowSql(command, value, columnsToUpdateValues, checkConcurrency, false, ref paramIndex, "  ", sb);
            sb.AppendLine("END");
            sb.AppendLine("END CATCH;");
        }
    }

    #endregion Merges
}
