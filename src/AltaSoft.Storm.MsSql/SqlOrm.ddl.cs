using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Interfaces;
using Microsoft.Data.SqlClient;

namespace AltaSoft.Storm;

/// <summary>
/// A static class that provides functionality for working with SQL databases using object-relational mapping (ORM) techniques.
/// </summary>
public static partial class SqlOrm
{
    /// <summary>
    /// A string constant that represents a comma followed by a new line and a space.
    /// </summary>
    private static readonly string s_commaNewLineAndSpace = ',' + Environment.NewLine + "  ";

    /// <summary>
    /// Sets the current database for the specified SqlConnection asynchronously.
    /// </summary>
    /// <param name="connection">The SqlConnection to use.</param>
    /// <param name="databaseName">The name of the database to set as the current database.</param>
    /// <param name="queryParameters">Optional parameters for the query.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A Task representing the asynchronous operation. The task result is the number of rows affected.</returns>
    public static async Task<int> UseDatabaseAsync(this SqlConnection connection, string databaseName, DdlParameters? queryParameters = null, CancellationToken cancellationToken = default)
    {
        queryParameters ??= new DdlParameters(connection);

        var sql = "USE " + databaseName.QuoteSqlName();

        await using var command = StormManager.CreateCommand(false);

        return await InternalExecuteNonQuery(command, sql, queryParameters, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a database asynchronously using the specified SqlConnection.
    /// </summary>
    /// <param name="connection">The SqlConnection object.</param>
    /// <param name="databaseName">The name of the database to create.</param>
    /// <param name="checkNotExists">A flag indicating whether to check if the database already exists before creating it.</param>
    /// <param name="queryParameters">Optional query parameters.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A Task representing the asynchronous operation, returning the number of rows affected.</returns>
    public static async Task<int> CreateDatabaseAsync(this SqlConnection connection, string databaseName, bool checkNotExists, DdlParameters? queryParameters = null, CancellationToken cancellationToken = default)
    {
        queryParameters ??= new DdlParameters(connection);
        var quotedDbName = databaseName.QuoteSqlName();

        var sb = new StringBuilder(256);
        if (checkNotExists)
            sb.Append("IF DB_ID(").Append(databaseName.QuoteName('\'')).AppendLine(") IS NULL");
        sb.Append("CREATE DATABASE ").AppendLine(quotedDbName);

        await using var command = StormManager.CreateCommand(false);

        return await InternalExecuteNonQuery(command, sb.ToString(), queryParameters, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a database asynchronously.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="databaseName">The name of the database to drop.</param>
    /// <param name="checkExists">Indicates whether to check if the database exists before dropping it.</param>
    /// <param name="queryParameters">Optional query parameters.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, returning the number of rows affected.</returns>
    public static async Task<int> DropDatabaseAsync(this SqlConnection connection, string databaseName, bool checkExists, DdlParameters? queryParameters = null, CancellationToken cancellationToken = default)
    {
        queryParameters ??= new DdlParameters(connection);
        var quotedDbName = databaseName.QuoteSqlName();

        var sb = new StringBuilder(256);
        if (checkExists)
            sb.Append("IF DB_ID(").Append(databaseName.QuoteName('\'')).AppendLine(") IS NOT NULL").AppendLine("BEGIN");

        sb.Append("ALTER DATABASE ").Append(quotedDbName).AppendLine(" SET SINGLE_USER WITH ROLLBACK IMMEDIATE;");
        sb.Append("DROP DATABASE ").AppendLine(quotedDbName);

        if (checkExists)
            sb.Append("END");

        await using var command = StormManager.CreateCommand(false);

        return await InternalExecuteNonQuery(command, sb.ToString(), queryParameters, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a schema in the database asynchronously.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="schemaName">The name of the schema to create.</param>
    /// <param name="checkNotExists">Indicates whether to check if the schema already exists before creating it.</param>
    /// <param name="queryParameters">Optional query parameters.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation. The task result is the number of rows affected.</returns>
    public static async Task<int> CreateSchemaAsync(this SqlConnection connection, string schemaName, bool checkNotExists, DdlParameters? queryParameters = null, CancellationToken cancellationToken = default)
    {
        queryParameters ??= new DdlParameters(connection);
        var quotedSchemaName = schemaName.QuoteSqlName();

        var sb = new StringBuilder(256);
        if (checkNotExists)
        {
            sb.Append("IF SCHEMA_ID(").Append(schemaName.QuoteName('\'')).AppendLine(") IS NULL");
            sb.Append("EXEC ('CREATE SCHEMA ").Append(quotedSchemaName).AppendLine(" AUTHORIZATION [dbo]')");
        }
        else
        {
            sb.Append("CREATE SCHEMA ").AppendLine(quotedSchemaName);
        }

        await using var command = StormManager.CreateCommand(false);

        return await InternalExecuteNonQuery(command, sb.ToString(), queryParameters, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a database schema asynchronously.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="schemaName">The name of the schema to drop.</param>
    /// <param name="checkExists">Specifies whether to check if the schema exists before dropping it.</param>
    /// <param name="queryParameters">Optional query parameters.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation. The task result is the number of rows affected.</returns>
    public static async Task<int> DropSchemaAsync(this SqlConnection connection, string schemaName, bool checkExists, DdlParameters? queryParameters = null, CancellationToken cancellationToken = default)
    {
        queryParameters ??= new DdlParameters(connection);
        var quotedSchemaName = schemaName.QuoteSqlName();

        var sb = new StringBuilder(256);
        if (checkExists)
            sb.Append("IF SCHEMA_ID(").Append(schemaName.QuoteName('\'')).AppendLine(") IS NOT NULL").AppendLine("BEGIN");

        sb.Append("DROP SCHEMA ").AppendLine(quotedSchemaName);

        if (checkExists)
            sb.Append("END");

        await using var command = StormManager.CreateCommand(false);

        return await InternalExecuteNonQuery(command, sb.ToString(), queryParameters, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a table asynchronously in the database using the provided connection.
    /// </summary>
    /// <typeparam name="T">The type of the data model for the table.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="checkNotExists">A flag indicating whether to check if the table already exists before creating it.</param>
    /// <param name="createDetailTables">A flag indicating whether to create detail tables as well.</param>
    /// <param name="queryParameters">Optional query parameters.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="quotedTableFullName"> Optional fully-qualified, SQL-quoted table name (e.g., [dbo].[MyTable]). If not provided, the value is resolved from <typeparamref name="T"/>.
    /// </param>
    /// <param name="unquotedSchemaName"> Optional schema name without SQL quoting, used in constraint naming. If not provided, the value is resolved from <typeparamref name="T"/>.
    /// </param>
    /// <param name="unquotedTableName"> Optional table name without SQL quoting, used in constraint naming. If not provided, the value is resolved from <typeparamref name="T"/>.
    /// </param>
    /// <returns>A task representing the asynchronous operation. The task result is the number of rows affected.</returns>
    public static async Task<int> CreateTableAsync<T>(this SqlConnection connection, bool checkNotExists, bool createDetailTables = true, DdlParameters? queryParameters = null,
        string? unquotedSchemaName = null, string? unquotedTableName = null, CancellationToken cancellationToken = default)
            where T : IDataBindable
    {
        queryParameters ??= new DdlParameters(connection);

        var ctrl = StormControllerCache.Get<T>(0); // Other variants?
        var quotedSchemaName = unquotedSchemaName is not null ? unquotedSchemaName.QuoteSqlName() : ctrl.QuotedSchemaName;
        var quotedTableFullName = unquotedSchemaName is not null && unquotedTableName is not null ? unquotedSchemaName.QuoteSqlName() + "." + unquotedTableName.QuoteSqlName() : ctrl.QuotedObjectFullName;

        unquotedSchemaName ??= ctrl.QuotedSchemaName.UnquoteSqlName();
        unquotedTableName ??= ctrl.QuotedObjectName.UnquoteSqlName();
        var columns = ctrl.ColumnDefs;


        var sb = new StringBuilder(512);

        if (checkNotExists)
            sb.Append("IF OBJECT_ID (").Append(quotedTableFullName.QuoteName('\'')).AppendLine(") IS NULL");

        sb.Append("CREATE TABLE ").AppendLine(quotedTableFullName).Append('(').AppendLine();
        sb.Append(' ', 2)
            .AppendJoinFast(columns, s_commaNewLineAndSpace,
                x => x.SaveAs != SaveAs.DetailTable && x.Flags != StormColumnFlags.None,
                static (builder, x) => builder.Append(GetSqlColumnInfo(x)));
        sb.Append(',').AppendLine();

        var pkColumns = columns.FilterAndSelectList(
            static x => x.SaveAs != SaveAs.DetailTable && (x.Flags & StormColumnFlags.Key) != 0,
            static x => x.ColumnName);

        sb.Append("  CONSTRAINT [PK_").Append(unquotedSchemaName).Append('_').Append(unquotedTableName).Append("] PRIMARY KEY CLUSTERED (");
        sb.AppendJoinFast(',', pkColumns).Append(')').AppendLine();
        sb.AppendLine(");");

        if (createDetailTables)
        {
            foreach (var columnDef in columns.GetDetailColumns())
            {
                Debug.Assert(columnDef.QuotedDetailTableName is not null);

                StormColumnDef[] detColumns;

                if (columnDef.DbType != UnifiedDbType.Default) // Primitive list item binding is specified
                {
                    detColumns = [
                        new StormColumnDef(columnDef.ColumnName, null, columnDef.ColumnName,
                         StormColumnFlags.Key | StormColumnFlags.CanSelect | StormColumnFlags.CanInsert | StormColumnFlags.CanUpdate,
                            columnDef.DbType, columnDef.Size, columnDef.Precision, columnDef.Scale, SaveAs.Default, 0, false,
                         null, null, columnDef.PropertyType, columnDef.PropertySerializationType)
                    ];
                }
                else
                {
                    var detCtrl = StormControllerCache.Get(columnDef.DetailType!, 0); //TODO other variants?
                    detColumns = detCtrl.ColumnDefs;
                }

                var detailTableFullName = quotedSchemaName + '.' + columnDef.QuotedDetailTableName;
                if (checkNotExists)
                    sb.Append("IF OBJECT_ID (").Append(detailTableFullName.QuoteName('\'')).AppendLine(") IS NULL");

                sb.Append("CREATE TABLE ").AppendLine(detailTableFullName).Append('(').AppendLine();

                // Master table key columns
                sb.Append(' ', 2)
                    .AppendJoinFast(columns, s_commaNewLineAndSpace, x => x.IsKey(),
                        static (builder, x) => builder.Append(GetSqlColumnInfo(x)));
                sb.Append(',').AppendLine();

                // Detail table columns
                sb.Append(' ', 2)
                    .AppendJoinFast(detColumns, s_commaNewLineAndSpace,
                        x => x.Flags != StormColumnFlags.None,
                        static (builder, x) => builder.Append(GetSqlColumnInfo(x)));
                sb.Append(',').AppendLine();

                // Primary key
                sb.Append("  CONSTRAINT [PK_").Append(unquotedSchemaName).Append('_').Append(columnDef.UnquotedDetailTableName).Append("] PRIMARY KEY CLUSTERED (");

                // Master table keys
                sb.AppendJoinFast(',', pkColumns);
                sb.Append(',');

                // Detail table keys
                var oldLength = sb.Length;
                sb.AppendJoinFast(detColumns, ',', x => x.IsKey(),
                    static (builder, x) => builder.Append(x.ColumnName));
                if (oldLength == sb.Length) // No key columns added
                {
                    sb.AppendJoinFast(detColumns, ',', x => x.Flags != StormColumnFlags.None && !x.IsNullable,
                        static (builder, x) => builder.Append(x.ColumnName)); // Add all non nullable detail columns
                }
                sb.Append(')').AppendLine();
                sb.AppendLine(");");
            }
        }

        await using var command = StormManager.CreateCommand(false);

        return await InternalExecuteNonQuery(command, sb.ToString(), queryParameters, cancellationToken).ConfigureAwait(false);

        static string GetSqlColumnInfo(StormColumnDef column)
        {
            var typeName = column.IsRowVersion()
                ? "rowversion"
                : StormManager.ToSqlDbType(column.DbType, column.Size, column.Precision, column.Scale);

            return $"{column.ColumnName} {typeName} {(column.IsAutoInc() ? "IDENTITY " : "")}{(column.IsNullable ? "NULL" : "NOT NULL")}";
        }
    }

    /// <summary>
    /// Drops a table asynchronously from the database.
    /// </summary>
    /// <typeparam name="T">The type of the table.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="checkExists">Indicates whether to check if the table exists before dropping it.</param>
    /// <param name="dropDetailTables">Indicates whether to drop detail tables as well.</param>
    /// <param name="queryParameters">Optional query parameters.</param>
    /// <param name="quotedSchemaName"> Optional SQL-quoted schema name (e.g., <c>[dbo]</c>). If not provided, the table name is resolved from <typeparamref name="T"/>. </param>
    /// <param name="quotedTableFullName"> Optional fully-qualified, SQL-quoted table name (e.g., <c>[dbo].[MyTable]</c>). If not provided, the table name is resolved from <typeparamref name="T"/>. </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the number of rows affected.</returns>
    public static async Task<int> DropTableAsync<T>(this SqlConnection connection, bool checkExists, bool dropDetailTables = true, DdlParameters? queryParameters = null, string? quotedSchemaName = null, string? quotedTableFullName = null, CancellationToken cancellationToken = default)
            where T : IDataBindable
    {
        queryParameters ??= new DdlParameters(connection);

        var ctrl = StormControllerCache.Get<T>(0); // Other variants?
        quotedTableFullName ??= ctrl.QuotedObjectFullName;
        quotedSchemaName ??= ctrl.QuotedSchemaName;

        var sb = new StringBuilder(256);

        if (checkExists)
            sb.Append("IF OBJECT_ID (").Append(quotedTableFullName.QuoteName('\'')).AppendLine(") IS NOT NULL").Append(' ', 2);
        sb.Append(" DROP TABLE ").AppendLine(quotedTableFullName);

        if (dropDetailTables)
        {
            foreach (var quotedDetailTableName in ctrl.ColumnDefs.GetDetailColumns().Select(x => x.QuotedDetailTableName))
            {
                Debug.Assert(quotedDetailTableName is not null);

                var detailTableFullName = quotedSchemaName + '.' + quotedDetailTableName;
                if (checkExists)
                    sb.Append("IF OBJECT_ID (").Append(detailTableFullName.QuoteName('\'')).AppendLine(") IS NOT NULL").Append(' ', 2);

                sb.Append(" DROP TABLE ").AppendLine(detailTableFullName);
            }
        }

        await using var command = StormManager.CreateCommand(false);

        return await InternalExecuteNonQuery(command, sb.ToString(), queryParameters, cancellationToken).ConfigureAwait(false);
    }
}
