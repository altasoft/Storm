using Microsoft.Extensions.Logging;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AltaSoft.Storm;

public static partial class PostgreSqlOrm
{
    private static readonly QueryParameters s_defaultQueryParameters = new();
    private static readonly UpdateParameters s_defaultUpdateParameters = new();

    private static async Task<T> InternalExecuteScalarQuery<T>(DbConnection conn, DbCommand command, string sql, OrmParameters queryParameters, CancellationToken cancellationToken)
    {
        SetCommandParameters(command, sql, conn, queryParameters.Transaction, queryParameters.CommandTimeout);

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        return (T)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    private static async Task<int> InternalExecuteNonQuery(DbConnection conn, DbCommand command, string sql, OrmParameters queryParameters, CancellationToken cancellationToken)
    {
        SetCommandParameters(command, sql, conn, queryParameters.Transaction, queryParameters.CommandTimeout);

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void SetCommandParameters(DbCommand command, string commandText, DbConnection conn, DbTransaction? transaction = null, int? commandTimeout = null, CommandType? commandType = null)
    {
        command.CommandText = commandText;
        command.CommandType = commandType ?? CommandType.Text;
        command.Connection = conn;
        command.Transaction = transaction;

        if (commandTimeout.HasValue)
            command.CommandTimeout = commandTimeout.Value;

        var logger = Storm.Logger;
        if (logger?.IsEnabled(LogLevel.Trace) == true)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < command.Parameters.Count; i++)
            {
                var p = command.Parameters[i];
                sb.Append("  ").Append(p.ParameterName).Append(": ").Append(p.Value is string s ? ('"' + s + '"') : p.Value).AppendLine();
            }
            logger.LogTrace("Generated sql query:\n{CommandText}\n{CommandParams}", commandText, sb.ToString());
        }
    }
}
