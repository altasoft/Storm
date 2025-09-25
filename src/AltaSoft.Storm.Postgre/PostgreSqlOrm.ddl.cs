using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm;

public static partial class PostgreSqlOrm
{
    public static async Task<int> CreateDatabaseAsync(this DbConnection self, string databaseName, bool checkNotExists, OrmParameters? queryParameters = null, CancellationToken cancellationToken = default)
    {
        var quotedDbName = databaseName.QuoteSqlName();

        if (checkNotExists)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT 1 FROM pg_database WHERE datname = ").Append(databaseName.QuoteName('\'')).AppendLine(")");
            await using var command = self.CreateCommand();
            var r = await InternalExecuteScalarQuery<int?>(self, command, sb.ToString(), queryParameters ?? s_defaultQueryParameters, cancellationToken).ConfigureAwait(false);
            if (r is null)
                return -1;
        }

        await using var command2 = self.CreateCommand();
        return await InternalExecuteNonQuery(self, command2, "CREATE DATABASE " + quotedDbName, queryParameters ?? s_defaultQueryParameters, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<int> DropDatabaseAsync(this DbConnection self, string databaseName, bool checkExists, OrmParameters? queryParameters = null, CancellationToken cancellationToken = default)
    {
        var quotedDbName = databaseName.QuoteSqlName();

        var sb = new StringBuilder();
        sb.Append("DROP DATABASE ");
        if (checkExists)
            sb.Append("IF EXISTS ");
        sb.AppendLine(quotedDbName);

        await using var command = self.CreateCommand();
        return await InternalExecuteNonQuery(self, command, sb.ToString(), queryParameters ?? s_defaultQueryParameters, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<int> CreateSchemaAsync(this DbConnection self, string schemaName, bool checkNotExists, OrmParameters? queryParameters = null, CancellationToken cancellationToken = default)
    {
        var quotedSchemaName = schemaName.QuoteSqlName();

        var sb = new StringBuilder();
        sb.Append("CREATE SCHEMA ");
        if (checkNotExists)
            sb.Append("IF NOT EXISTS ");
        sb.AppendLine(quotedSchemaName);

        await using var command = self.CreateCommand();
        return await InternalExecuteNonQuery(self, command, sb.ToString(), queryParameters ?? s_defaultQueryParameters, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<int> DropSchemaAsync(this DbConnection self, string schemaName, bool checkExists, OrmParameters? queryParameters = null, CancellationToken cancellationToken = default)
    {
        var quotedSchemaName = schemaName.QuoteSqlName();

        var sb = new StringBuilder();
        sb.Append("DROP SCHEMA ");
        if (checkExists)
            sb.Append("IF EXISTS ");
        sb.AppendLine(quotedSchemaName);

        await using var command = self.CreateCommand();
        return await InternalExecuteNonQuery(self, command, sb.ToString(), queryParameters ?? s_defaultQueryParameters, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<int> CreateTableAsync<T>(this DbConnection self, bool checkNotExists, OrmParameters? queryParameters = null, CancellationToken cancellationToken = default)
        where T : IDataBindable, IChangeTrackable
    {
        var ctrl = StormControllerCache.Get<T>();
        var quotedTableFullName = ctrl.QuotedTableFullName;
        var unquotedSchemaName = ctrl.UnquotedSchemaName;
        var unquotedTableName = ctrl.UnquotedTableName;
        var columnDefs = ctrl.ColumnDefs;

        var sb = new StringBuilder();

        sb.Append("CREATE TABLE ");
        if (checkNotExists)
            sb.Append("IF NOT EXISTS ");
        sb.AppendLine(quotedTableFullName).AppendLine("(");
        sb.Append(' ', 2).AppendJoin(",\n\r  ",
            columnDefs.Select(x => $"{x.ColumnName} {Storm.ToSqlDbTypeFunc(x.DbType, x.Size)}{GetSizeString(x.DbType, x.Size)} {(x.IsNullable ? "NULL" : "NOT NULL")}"));
        sb.AppendLine();

        sb.Append("  CONSTRAINT [PK_").Append(unquotedSchemaName);
        sb.Append('_').Append(unquotedTableName).Append("] PRIMARY KEY CLUSTERED (");
        sb.AppendJoin(", ", columnDefs.Where(x => (x.Flags & StormColumnFlags.Key) != 0).Select(x => x.ColumnName)).AppendLine(")");

        sb.AppendLine(")");

        await using var command = self.CreateCommand();
        return await InternalExecuteNonQuery(self, command, sb.ToString(), queryParameters ?? s_defaultQueryParameters, cancellationToken).ConfigureAwait(false);

        static string GetSizeString(UnifiedDbType _, int size)
        {
            return size switch
            {
                -1 => "(max)",
                > 0 => "(" + size.ToString(CultureInfo.InvariantCulture) + ")",
                _ => string.Empty
            };
        }
    }

    public static async Task<int> DropTableAsync<T>(this DbConnection self, bool checkExists, OrmParameters? queryParameters = null, CancellationToken cancellationToken = default)
           where T : IDataBindable, IChangeTrackable
    {
        var ctrl = StormControllerCache.Get<T>();
        var quotedTableFullName = ctrl.QuotedTableFullName;

        var sb = new StringBuilder();

        sb.Append("DROP TABLE ");
        if (checkExists)
            sb.Append("IF EXISTS ");
        sb.AppendLine(quotedTableFullName);

        await using var command = self.CreateCommand();
        return await InternalExecuteNonQuery(self, command, sb.ToString(), queryParameters ?? s_defaultQueryParameters, cancellationToken).ConfigureAwait(false);
    }
}
