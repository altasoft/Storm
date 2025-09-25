using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Generator.Common;
using AltaSoft.Storm.Models;

namespace AltaSoft.Storm.Helpers;

internal static class Generators
{
    private static readonly string s_commaNewLineAndSpace = ',' + Environment.NewLine + "  ";

    public static string GenerateCSharpClass(DbObjectDef dbObject)
    {
        var attrList = new List<string>(8);

        var objectType = dbObject.ObjectType;

        var builder = new SourceCodeBuilder();

        builder.Append("[StormDbObject(SchemaName = \"").Append(dbObject.SchemaName)
            .Append("\", ObjectName = \"").Append(dbObject.ObjectName).Append("\"")
            .AppendIf(objectType != DupDbObjectType.Table, $", ObjectType = ObjectType.{objectType}").AppendLine(")]");

        foreach (var indexInfo in dbObject.Indexes)
        {
            builder.Append("[StormIndex([").Append(string.Join(", ", indexInfo.Columns.Select(x => '"' + x + '"'))).Append("], ")
                .Append(indexInfo.IsUnique ? "true" : "false")
                .Append(", \"").Append(indexInfo.IndexName).AppendLine("\")]");
        }

        builder.AppendClass(true, "public partial", dbObject.ObjectName.ToPascalCase());

        foreach (var columnInfo in dbObject.Columns)
        {
            var propertyName = columnInfo.ColumnName.ToPascalCase();

            var propertyType = columnInfo.DataType.ToDotNetType();

            attrList.Clear();

            if (propertyName != columnInfo.ColumnName)
            {
                attrList.Add($"ColumnName = \"{columnInfo.ColumnName}\"");
            }

            var columnTypeFlags = new List<string>(4);
            if (columnInfo.IsKey)
            {
                columnTypeFlags.Add("ColumnType.PrimaryKey");
            }
            if (columnInfo.IsAutoIncrement)
            {
                columnTypeFlags.Add("ColumnType.AutoIncrement");
            }
            if (columnInfo.DataType.Equals("timestamp", StringComparison.Ordinal) || columnInfo.DataType.Equals("rowversion", StringComparison.Ordinal))
            {
                columnTypeFlags.Add("ColumnType.RowVersion");
                columnTypeFlags.Add("ColumnType.ConcurrencyCheck");
            }
            if (columnInfo.DefaultValue is not null)
            {
                columnTypeFlags.Add("ColumnType.HasDefaultValue");
            }

            if (columnTypeFlags.Count > 0)
            {
                attrList.Add("ColumnType = " + string.Join(" | ", columnTypeFlags));
            }

            var dbType = columnInfo.DataType.ToUnifiedDbType();

            if (columnInfo.DataType is "char" or "varchar" or "nchar" or "nvarchar")
            {
                attrList.Add("DbType = UnifiedDbType." + dbType);
                attrList.Add("Size = " + (columnInfo.CharacterMaximumLength ?? -1).ToString(CultureInfo.InvariantCulture));
            }
            else
            if (columnInfo.DataType is "money" or "smallmoney")
            {
                attrList.Add("DbType = UnifiedDbType." + dbType);
            }
            else
            if (columnInfo.DataType is "datetime2" or "time" or "datetimeoffset")
            {
                attrList.Add("DbType = UnifiedDbType." + dbType);
                attrList.Add("Precision = " + (columnInfo.NumericPrecision ?? 7).ToString(CultureInfo.InvariantCulture));
            }
            else
            if (columnInfo.DataType is "decimal" or "numeric")
            {
                attrList.Add("DbType = UnifiedDbType." + dbType);
                attrList.Add("Precision = " + (columnInfo.NumericPrecision ?? 18).ToString(CultureInfo.InvariantCulture));
                attrList.Add("Scale = " + (columnInfo.NumericScale ?? 0).ToString(CultureInfo.InvariantCulture));
            }
            if (columnInfo.DataType is "float")
            {
                attrList.Add("DbType = UnifiedDbType." + dbType);
                attrList.Add("Precision = " + (columnInfo.NumericPrecision ?? 53).ToString(CultureInfo.InvariantCulture));
            }

            if (attrList.Count > 0)
            {
                builder.Append("[StormColumn(");
                builder.Append(string.Join(", ", attrList));
                builder.AppendLine(")]");
            }

            builder.Append("public ").Append(propertyType).AppendIf(columnInfo.IsNullable, "?").Append(" ")
                .Append(propertyName).AppendLine(" { get; set; }");
        }

        builder.CloseBracket();

        return builder.ToString();
    }

    public static string GenerateCreateTableSql(StormTypeDef type, string unquotedSchemaName, bool createDetailTables)
    {
        var sb = new StringBuilder(512);
        var unquotedTableName = type.GetObjectName();

        var quotedSchemaName = unquotedSchemaName.QuoteSqlName();
        var quotedTableName = unquotedTableName.QuoteSqlName();

        var quotedTableFullName = quotedSchemaName + '.' + quotedTableName;

        sb.Append("IF OBJECT_ID (").Append(quotedTableFullName.QuoteName('\'')).AppendLine(") IS NULL");

        var tableColumns = new List<string>(16);
        var pkColumns = new List<string>(4);

        foreach (var prop in type.Properties)
        {
            var binding = prop.GetEffectiveBindColumnData();

            if (binding.SaveAs == DupSaveAs.DetailTable)
                continue;

            if (binding.SaveAs == DupSaveAs.FlatObject)
            {
                if (prop.Details is null)
                    throw new InvalidOperationException("Flat object property should have details");

                foreach (var detProp in prop.Details)
                {
                    var detBinding = detProp.GetEffectiveBindColumnData();

                    if (detBinding.IsKey || !detProp.PropertyGenSpec.IsReadOnly)
                    {
                        tableColumns.Add(GetSqlColumnInfo(detProp, detProp.GetEffectiveBindColumnData()));
                    }
                }
                continue;
            }

            if (binding.IsKey || !prop.PropertyGenSpec.IsReadOnly)
            {
                tableColumns.Add(GetSqlColumnInfo(prop, binding));

                if (binding.IsKey)
                {
                    pkColumns.Add(prop.GetColumnName().QuoteSqlName());
                }
            }
        }

        sb.Append("CREATE TABLE ").AppendLine(quotedTableFullName).Append('(').AppendLine();
        sb.Append(' ', 2).Append(string.Join(s_commaNewLineAndSpace, tableColumns));
        sb.Append(',').AppendLine();

        sb.Append("  CONSTRAINT [PK_").Append(unquotedSchemaName).Append('_').Append(unquotedTableName).Append("] PRIMARY KEY CLUSTERED (");
        sb.Append(string.Join(", ", pkColumns)).Append(')').AppendLine();

        sb.AppendLine(");");
        sb.AppendLine("GO");
        sb.AppendLine();

        if (!createDetailTables)
            return sb.ToString();

        foreach (var prop in type.Properties.Where(x => x.GetEffectiveBindColumnData().SaveAs == DupSaveAs.DetailTable))
        {
            var binding = prop.GetEffectiveBindColumnData();

            var detailTableFullName = quotedSchemaName + '.' + binding.DetailTableName!.QuoteSqlName();
            sb.Append("IF OBJECT_ID (").Append(detailTableFullName.QuoteName('\'')).AppendLine(") IS NULL");

            sb.Append("CREATE TABLE ").AppendLine(detailTableFullName).Append('(').AppendLine();

            // Detail table columns
            sb.Append(' ', 2).Append(string.Join(s_commaNewLineAndSpace,
                prop.Details!.Where(x => x.GetEffectiveBindColumnData().IsKey || !x.PropertyGenSpec.IsReadOnly).Select(x => GetSqlColumnInfo(x, x.GetEffectiveBindColumnData()))));
            sb.Append(',').AppendLine();

            // Primary key
            sb.Append("  CONSTRAINT [PK_").Append(unquotedSchemaName).Append('_').Append(binding.DetailTableName).Append("] PRIMARY KEY CLUSTERED (");

            // Master table keys
            sb.Append(string.Join(", ", type.Properties
                .Where(x => x.GetEffectiveBindColumnData().SaveAs != DupSaveAs.DetailTable && x.GetEffectiveBindColumnData().IsKey)
                .Select(x => x.GetEffectiveBindColumnData().ColumnName!.QuoteSqlName())));
            sb.Append(',');

            // Detail table keys
            var oldLength = sb.Length;
            sb.Append(string.Join(", ", prop.Details!.Where(x => !x.IsMasterDetailColumn && x.GetEffectiveBindColumnData().IsKey).Select(x => x.GetEffectiveBindColumnData().ColumnName!.QuoteSqlName())));
            if (oldLength == sb.Length) // No key columns added
                sb.Append(string.Join(",", prop.Details!.Where(x => !x.IsMasterDetailColumn && !x.IsNullable).Select(x => x.GetEffectiveBindColumnData().ColumnName!.QuoteSqlName()))); // Add all non nullable detail columns
            sb.Append(')').AppendLine();
            sb.AppendLine(");");
            sb.AppendLine("GO");
            sb.AppendLine();
        }
        return sb.ToString();

        static string GetSqlColumnInfo(StormPropertyDef p, BindColumnData binding)
        {
            return $"{p.GetColumnName().QuoteSqlName()} {binding.DbType!.Value.ToSqlDbTypeText(binding.Size!.Value,
                binding.Precision!.Value, binding.Scale!.Value)} {(p.IsNullable ? "NULL" : "NOT NULL")}";
        }
    }
}
