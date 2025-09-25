using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Extensions;
using Microsoft.Data.SqlClient;

namespace AltaSoft.Storm;

//TODO https://github.com/dotnet/efcore/blob/main/src/EFCore.SqlServer/Storage/Internal/SqlServerTransientExceptionDetector.cs

public static partial class SqlOrm
{
    private static async Task<int> InternalExecuteNonQuery(SqlCommand command, string sql, DdlParameters queryParameters, CancellationToken cancellationToken)
    {
        var connection = queryParameters.Connection;

        command.Connection = queryParameters.Connection;
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        command.Transaction = queryParameters.Transaction;

        if (queryParameters.CommandTimeout.HasValue)
            command.CommandTimeout = queryParameters.CommandTimeout.Value;

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        return await command.ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
    }
}
