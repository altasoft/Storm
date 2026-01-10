using System.Collections.Generic;
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
    internal async Task<int> DeleteAsync<T>(
        ModifyQueryParameters<T> queryParameters,
        CancellationToken cancellationToken = default)
        where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);

            PrepareDelete(vCommand, queryParameters);

            var (connection, transaction) = await queryParameters.Context.EnsureConnectionAndTransactionIsOpenAsync(cancellationToken).ConfigureAwait(false);
            command.SetStormCommandBaseParameters(connection, transaction);

            return await command.ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<int> DeleteAsync<T>(T value, ModifyQueryParameters<T> queryParameters, CancellationToken cancellationToken = default)
        where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);

            PrepareDelete(vCommand, value, queryParameters);

            var (connection, transaction) = await queryParameters.Context.EnsureConnectionAndTransactionIsOpenAsync(cancellationToken).ConfigureAwait(false);
            command.SetStormCommandBaseParameters(connection, transaction);

            return await command.ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal async Task<int> DeleteAsync<T>(IEnumerable<T> valueList, ModifyQueryParameters<T> queryParameters, CancellationToken cancellationToken = default)
        where T : IDataBindable
    {
        var command = StormManager.CreateCommand(false);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);

            PrepareDelete(vCommand, valueList, queryParameters);
            if (vCommand.CommandText.Length == 0)
                return -1;

            var (connection, transaction) = await queryParameters.Context.EnsureConnectionAndTransactionIsOpenAsync(cancellationToken).ConfigureAwait(false);
            command.SetStormCommandBaseParameters(connection, transaction);

            return await command.ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    #region Deletes

    internal void PrepareDelete<T>(
        IVirtualStormDbCommand command,
        ModifyQueryParameters<T> queryParameters)
        where T : IDataBindable
    {
        var sb = StormManager.GetStringBuilderFromPool();

        GenerateDeleteSql(command, queryParameters, sb);

        command.SetStormCommandBaseParameters(sb.ToStringAndReturnToPool(), queryParameters);
    }

    internal void PrepareDelete<T>(
        IVirtualStormDbCommand command,
        T value,
        ModifyQueryParameters<T> queryParameters) where T : IDataBindable
    {
        var sb = StormManager.GetStringBuilderFromPool();

        var paramIndex = 1;
        GenerateDeleteSql(command, value, ref paramIndex, sb);

        command.SetStormCommandBaseParameters(sb.ToStringAndReturnToPool(), queryParameters);
    }

    internal void PrepareDelete<T>(
        IVirtualStormDbCommand command,
        IEnumerable<T> valueList,
        ModifyQueryParameters<T> queryParameters)
        where T : IDataBindable
    {
        var sb = StormManager.GetStringBuilderFromPool();

        var paramIndex = 1;
        foreach (var value in valueList)
        {
            GenerateDeleteSql(command, value, ref paramIndex, sb);
        }

        if (sb.Length == 0)
        {
            StormManager.ReturnStringBuilderToPool(sb);
            return;
        }

        if (!queryParameters.Context.IsInTransactionScope)
        {
            sb.WrapIntoBeginTranCommit();
        }

        command.SetStormCommandBaseParameters(sb.ToStringAndReturnToPool(), queryParameters);
    }

    private void GenerateDeleteSql<T>(IVirtualStormDbCommand command, IKeyAndWhereExpression<T> queryParameters, StringBuilder sb) where T : IDataBindable
    {
        var paramIndex = 1;
        var (startIndex, len) = (-1, 0);

        sb.AppendLine("SET NOCOUNT ON;");

        foreach (var column in ColumnDefs.GetDetailColumns())
        {
            sb.Append("DELETE FROM ").Append(QuotedSchemaName).Append('.').Append(column.QuotedDetailTableName);
            if (startIndex < 0)
            {
                (startIndex, len) = GenerateSqlWhere(command, queryParameters, null, ref paramIndex, sb);
            }
            sb.AppendLine();
        }

        sb.AppendLine("SET NOCOUNT OFF;");
        sb.Append("DELETE FROM ").Append(QuotedObjectFullName);

        if (startIndex < 0)
        {
            GenerateSqlWhere(command, queryParameters, null, ref paramIndex, sb);
        }
        else
        if (len > 0)
        {
            sb.Append(sb, startIndex, len);
        }
        sb.AppendLine();
    }

    /// <summary>
    /// Generates an SQL DELETE command for a given value and appends it to a StringBuilder.
    /// </summary>
    /// <param name="command">The database command object to which parameters will be added.</param>
    /// <param name="value">The value implementing IDataBindable from which to derive the conditions for deletion.</param>
    /// <param name="paramIndex">The starting index for parameterization in the SQL command.</param>
    /// <param name="sb">The StringBuilder to which the generated SQL will be appended.</param>
    /// <returns>The StringBuilder with the appended SQL DELETE command.</returns>
    private void GenerateDeleteSql(IVirtualStormDbCommand command, IDataBindable value, ref int paramIndex, StringBuilder sb)
    {
        value.BeforeSave(SaveAction.Delete);

        var (whereStatements, _, _) = GetPkInformation(command, value, ref paramIndex);

        sb.AppendLine("SET NOCOUNT ON;");

        foreach (var column in ColumnDefs.GetDetailColumns())
        {
            sb.Append("DELETE FROM ").Append(QuotedSchemaName).Append('.').AppendLine(column.QuotedDetailTableName);
            sb.Append("WHERE ").AppendJoinFast(" AND ", whereStatements).AppendLine();
        }

        sb.AppendLine("SET NOCOUNT OFF;");
        sb.Append("DELETE FROM ").AppendLine(QuotedObjectFullName);
        sb.Append("WHERE ").AppendJoinFast(" AND ", whereStatements).AppendLine();
    }

    /// <summary>
    /// Generates the SQL statement for deleting detail records.
    /// </summary>
    private void GenerateDeleteDetailSql(IVirtualStormDbCommand command, string detailTableName,
        IDataBindable detailValue, List<string> masterPkWhereStatements, ref int paramIndex, StringBuilder sb)
    {
        detailValue.BeforeSave(SaveAction.Delete);

        var pid = paramIndex;

        var (whereStatements, _, _) = GetPkInformation(command, detailValue, ref pid);

        sb.Append("DELETE FROM ").Append(QuotedSchemaName).Append('.').AppendLine(detailTableName);
        sb.Append("WHERE ").AppendJoinFast(" AND ", masterPkWhereStatements, whereStatements);
        sb.AppendLine();

        paramIndex = pid;
    }

    #endregion Deletes
}
