using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Generator.Common;
using AltaSoft.Storm.Models;
using Microsoft.Data.SqlClient;

namespace AltaSoft.Storm.Helpers;

internal static class DbHelper
{
    public static async Task<DbEntityDefList> GetDbEntitiesAsync(ConnectionData connectionData, Action<string, int, int> onProgress, CancellationToken cancellationToken)
    {
        //TODO
        //_connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Integrated Security=True;Encrypt=false;TrustServerCertificate=true";

        onProgress($"Connecting to {connectionData.ConnectionString}", -1, 100);

        var tables = new DbEntityDef("Tables", DupDbObjectType.Table);
        var views = new DbEntityDef("Views", DupDbObjectType.View);
        var tvFuncs = new DbEntityDef("Table-valued Functions", DupDbObjectType.TableValuedFunction);
        var procs = new DbEntityDef("Procedures", DupDbObjectType.StoredProcedure);
        var svFuncs = new DbEntityDef("Scalar-valued Functions", DupDbObjectType.ScalarValuedFunction);

        var result = new DbEntityDefList { tables, views, tvFuncs, procs, svFuncs };

        const string sqlDbObjects =
            """
            select o.[object_id], s.[name] as [schema_name], o.[name], o.[type]
            FROM sys.objects o
              INNER JOIN sys.schemas s ON o.[schema_id] = s.[schema_id]
            WHERE o.[type] IN ('U', 'V', 'TF', 'IF', 'P', 'FN')
            ORDER BY s.[name], o.[name]
            """;

        const string sqlColumns =
            """
            SELECT t.[object_id], col.[name], col.is_nullable, typ.[name] AS data_type,
                CASE 
                    WHEN typ.[name] IN ('nvarchar', 'nchar') THEN col.max_length / 2 
                    ELSE col.max_length 
                END AS max_length,
                CASE 
                    WHEN typ.[name] IN ('decimal', 'numeric') THEN col.[precision]
                    WHEN typ.[name] IN ('datetime2', 'datetimeoffset', 'time') THEN col.scale
                    ELSE NULL
                END AS [precision],
                CASE 
                    WHEN typ.[name] IN ('decimal', 'numeric') THEN col.scale
                    ELSE NULL
                END AS [scale],
                CASE WHEN i.index_id IS NOT NULL THEN 1 ELSE 0 END AS is_part_of_pk,
                col.is_identity, object_definition(col.default_object_id) AS default_value
            FROM sys.columns AS col
                INNER JOIN sys.objects AS t ON col.[object_id] = t.[object_id]
                INNER JOIN sys.types AS typ ON col.system_type_id = typ.user_type_id
                LEFT JOIN sys.index_columns AS ic ON ic.[object_id] = t.[object_id] AND  ic.column_id = col.column_id
                LEFT JOIN sys.indexes AS i ON i.[object_id] = t.[object_id] AND ic.index_id = i.index_id AND i.is_primary_key = 1
            WHERE t.[type] IN ('U', 'V', 'TF', 'IF')
            ORDER BY t.[object_id], col.column_id
            
            """;

        const string sqlIndexes =
            """
            SELECT t.[object_id], idx.[name], idx.is_unique, STRING_AGG(col.[name], '|') WITHIN GROUP (ORDER BY ic.key_ordinal) AS [index_columns]
            FROM sys.indexes AS idx
                INNER JOIN sys.objects AS t ON idx.[object_id] = t.[object_id]
                INNER JOIN sys.index_columns AS ic ON idx.[object_id] = ic.[object_id] AND idx.index_id = ic.index_id
                INNER JOIN sys.indexes AS i ON i.[object_id] = t.[object_id] AND ic.index_id = i.index_id AND i.is_primary_key = 0
            	INNER JOIN sys.columns AS col ON ic.object_id = col.[object_id] AND ic.column_id = col.column_id
            WHERE t.[type] IN ('U', 'V', 'TF', 'IF')
            GROUP BY t.[object_id], idx.[name], idx.is_unique
            ORDER BY t.[object_id], idx.[name]
            """;

        using var connection = new SqlConnection(connectionData.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        onProgress("Getting list of database objects", -1, 100);

        var objectDict = new Dictionary<int, DbObjectDef>(128);

        // Tables, Views, Functions, Procedures, ...
        using (var command = new SqlCommand(sqlDbObjects, connection))
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var dbObject = new DbObjectDef(reader);
                objectDict.Add(dbObject.Id, dbObject);

                switch (dbObject.ObjectType)
                {
                    case DupDbObjectType.Table:
                        tables.DbObjects.Add(dbObject);
                        break;
                    case DupDbObjectType.View:
                        views.DbObjects.Add(dbObject);
                        break;
                    case DupDbObjectType.TableValuedFunction:
                        tvFuncs.DbObjects.Add(dbObject);
                        break;
                    case DupDbObjectType.StoredProcedure:
                        procs.DbObjects.Add(dbObject);
                        break;
                    case DupDbObjectType.ScalarValuedFunction:
                        svFuncs.DbObjects.Add(dbObject);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        // Columns
        using (var command = new SqlCommand(sqlColumns, connection))
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var dbColumn = new DbColumnDef(reader);
                if (objectDict.TryGetValue(dbColumn.Id, out var dbObject))
                {
                    if (ReferenceEquals(dbObject.Columns, DbObjectDef.DummyColumns))
                        dbObject.Columns = [];
                    dbObject.Columns.Add(dbColumn);
                }
            }
        }

        // Indexes
        using (var command = new SqlCommand(sqlIndexes, connection))
        {
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var dbIndex = new DbIndexDef(reader);
                if (objectDict.TryGetValue(dbIndex.Id, out var dbObject))
                {
                    if (ReferenceEquals(dbObject.Indexes, DbObjectDef.DummyIndexes))
                        dbObject.Indexes = [];
                    dbObject.Indexes.Add(dbIndex);
                }
            }
        }
        //onProgress("Done.", 100, 100);
        return result;
    }

    public static async Task GetProceduresAndFunctionsAsync(ConnectionData connectionData, StormTypeDefList stormTypes, DbEntityDefList dbEntities, Action<string, int, int> onProgress, CancellationToken cancellationToken)
    {
        using var conn = new SqlConnection(connectionData.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        foreach (var typeDef in stormTypes.Where(x => x.BindObjectData.ObjectType is not (DupDbObjectType.Table or DupDbObjectType.View or DupDbObjectType.VirtualView or DupDbObjectType.CustomSqlStatement)))
        {
            var dbObject = dbEntities.FindDbObject(typeDef.BindObjectData.ObjectType, typeDef.GetObjectName(), typeDef.BindObjectData.SchemaName);
            if (dbObject is null)
                continue;

            await FillColumnsAsync(conn, dbObject, typeDef.BindObjectData.ObjectType, cancellationToken);
        }

        onProgress("Done.", 100, 100);
    }

    public static async Task FillColumnsAsync(ConnectionData connectionData, DbObjectDef dbObject, DupDbObjectType objectType, CancellationToken cancellationToken)
    {
        using var conn = new SqlConnection(connectionData.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        await FillColumnsAsync(conn, dbObject, objectType, cancellationToken);
    }

    private static async Task FillColumnsAsync(SqlConnection conn, DbObjectDef dbObject, DupDbObjectType objectType, CancellationToken cancellationToken)
    {
        switch (objectType)
        {
            case DupDbObjectType.StoredProcedure:
                dbObject.Columns = await GetProceduresColumnInfoAsync(conn, dbObject.Id, cancellationToken);
                break;
            case DupDbObjectType.ScalarValuedFunction:
                dbObject.Columns = await GetScalarFunctionColumnInfoAsync(conn, dbObject.Id, cancellationToken);
                break;
        }
    }

    private static async Task<List<DbColumnDef>> GetProceduresColumnInfoAsync(SqlConnection conn, int dbObjectId, CancellationToken cancellationToken)
    {
        const string sql2 =
            """
            SELECT @object_id AS [object_id], ISNULL(col.[name], '') AS [name], col.is_nullable, typ.[name] AS data_type,
                CASE WHEN typ.[name] IN ('nvarchar', 'nchar') THEN col.max_length / 2 ELSE col.max_length END AS max_length,
                -- Precision
                CASE
                    WHEN typ.[name] IN ('decimal', 'numeric') THEN col.[precision]
                    WHEN typ.[name] IN ('datetime2', 'datetimeoffset', 'time') THEN col.scale
                    ELSE NULL
                END AS [precision],
                -- Scale
                CASE
                    WHEN typ.[name] IN ('decimal', 'numeric') THEN col.scale
                    ELSE NULL
                END AS [scale],
                0 AS is_part_of_pk,
                col.is_identity_column as is_identity, NULL AS default_value, col.column_ordinal AS OrderId
            FROM sys.dm_exec_describe_first_result_set_for_object(@object_id, 0) AS col
                INNER JOIN sys.types typ ON col.system_type_id = typ.user_type_id
            
            UNION ALL
            
            SELECT @object_id AS [object_id],
                CASE WHEN par.parameter_id = 0 THEN 'Return Value' COLLATE DATABASE_DEFAULT ELSE par.[name] END AS [name],
                CAST(1 AS bit) AS is_nullable, typ.[name] AS data_type,
                CASE WHEN typ.[name] IN ('nvarchar', 'nchar') THEN par.max_length / 2 ELSE par.max_length END AS max_length,
                -- Precision
                CASE
                    WHEN typ.[name] IN ('decimal', 'numeric') THEN par.[precision]
                    WHEN typ.[name] IN ('datetime2', 'datetimeoffset', 'time') THEN par.scale
                    ELSE NULL
                END AS [precision],
                -- Scale
                CASE
                    WHEN typ.[name] IN ('decimal', 'numeric') THEN par.scale
                    ELSE NULL
                END AS [scale],
                0 AS is_part_of_pk,
                CAST(0 AS bit) AS is_identity, NULL AS default_value, (10000000 + par.parameter_id) AS OrderId
            FROM sys.objects obj
                INNER JOIN sys.parameters par ON obj.[object_id] = par.[object_id]
                INNER JOIN sys.types typ ON par.system_type_id = typ.user_type_id
            WHERE obj.type = 'P' AND obj.[object_id] = @object_id
            ORDER BY OrderId;
            """;

        var columns = new List<DbColumnDef>(16);

        using var command = new SqlCommand(sql2, conn);
        command.Parameters.AddWithValue("@object_id", dbObjectId);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var dbColumn = new DbColumnDef(reader);
            columns.Add(dbColumn);
        }

        return columns;
    }

    private static async Task<List<DbColumnDef>> GetScalarFunctionColumnInfoAsync(SqlConnection conn, int dbObjectId, CancellationToken cancellationToken)
    {
        const string sql2 =
            """
            SELECT @object_id AS [object_id], 
                CASE WHEN par.parameter_id = 0 THEN 'Return Value' COLLATE DATABASE_DEFAULT ELSE par.[name] END AS [name], 
                CAST(1 AS bit) AS is_nullable, typ.[name] AS data_type,
                CASE WHEN typ.[name] IN ('nvarchar', 'nchar') THEN par.max_length / 2 ELSE par.max_length END AS max_length,
                -- Precision
                CASE
                    WHEN typ.[name] IN ('decimal', 'numeric') THEN par.[precision]
                    WHEN typ.[name] IN ('datetime2', 'datetimeoffset', 'time') THEN par.scale
                    ELSE NULL
                END AS [precision],
                -- Scale
                CASE
                    WHEN typ.[name] IN ('decimal', 'numeric') THEN par.scale
                    ELSE NULL
                END AS [scale],
                0 AS is_part_of_pk,
                CAST(0 AS bit) AS is_identity, NULL AS default_value
            FROM sys.objects obj
                INNER JOIN sys.parameters par ON obj.object_id = par.object_id
                INNER JOIN sys.types typ ON par.system_type_id = typ.user_type_id
            WHERE obj.type = 'FN' AND obj.[object_id] = @object_id
            ORDER BY par.parameter_id;
            """;

        var columns = new List<DbColumnDef>(16);

        using var command = new SqlCommand(sql2, conn);
        command.Parameters.AddWithValue("@object_id", dbObjectId);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var dbColumn = new DbColumnDef(reader);
            columns.Add(dbColumn);
        }

        return columns;
    }
}
