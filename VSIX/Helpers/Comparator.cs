using System;
using System.Linq;
using System.Threading;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Generator.Common;
using AltaSoft.Storm.Models;

namespace AltaSoft.Storm.Helpers;

internal static class Comparator
{
    public static void Compare(StormTypeDefList? projectTypes, DbEntityDefList? dbEntities, CancellationToken cancellationToken)
    {
        if (projectTypes is null || dbEntities is null)
            return;

        foreach (var typeDef in projectTypes)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            if (typeDef.BindObjectData.ObjectType is DupDbObjectType.VirtualView or DupDbObjectType.CustomSqlStatement)
                continue;

            var dbObject = dbEntities.FindDbObject(typeDef.BindObjectData.ObjectType, typeDef.GetObjectName(), typeDef.BindObjectData.SchemaName);
            if (dbObject is null)
            {
                typeDef.SetStatus(StormTypeStatus.TableNotFound, $"Corresponding database object '{typeDef.GetObjectNameWithSchema()}' not found");
                continue;
            }

            foreach (var prop in typeDef.Properties)
            {
                CompareProperty(prop, dbObject, dbEntities, typeDef.TypeSymbol.GetFullName(), typeDef.BindObjectData.SchemaName);
            }

            if (typeDef.Properties.Any(x => x.Status != StormPropertyStatus.Ok))
            {
                typeDef.SetStatus(StormTypeStatus.Warning, "Expand property to see details");
            }
            else
            {
                typeDef.SetStatus(StormTypeStatus.Ok, string.Empty);
            }
        }

        foreach (var dbEntity in dbEntities)
        {
            foreach (var dbObject in dbEntity.DbObjects)
            {
                var typeDef = projectTypes.FirstOrDefault(x =>
                    (x.BindObjectData.ObjectName ?? x.TypeSymbol.ToDisplayString()) == dbObject.ObjectName &&
                    (x.BindObjectData.SchemaName == dbObject.SchemaName || x.BindObjectData.SchemaName is null));

                if (typeDef is null)
                {
                    dbObject.SetStatus(DbObjectStatus.TypeNotFound, "Storm type (class, record or struct) missing");
                    continue;
                }

                if (typeDef.Status != StormTypeStatus.Ok)
                    dbObject.SetStatus(DbObjectStatus.Warning, "Please see corresponding Storm type for details");
                else
                    dbObject.SetStatus(DbObjectStatus.Ok, "");
            }
        }
    }

    private static void CompareProperty(StormPropertyDef prop, DbObjectDef dbObject, DbEntityDefList? dbEntities, string masterTypeFriendlyName, string? schemaName)
    {
        prop.SetStatus(StormPropertyStatus.Ok, string.Empty);

        if (prop.Details is not null) // Check flat object or details table
        {
            if ((prop.BindColumnData.SaveAs ?? prop.PropertyGenSpec.SaveAs) == DupSaveAs.DetailTable)
            {
                var detailTableName = prop.PropertyGenSpec.GetDetailTableName(masterTypeFriendlyName);

                var detailTable = schemaName is null
                        ? dbEntities?.FindDbObject(prop.ParentStormType.BindObjectData.ObjectType, x => IsSameDbObject(x.ObjectName, detailTableName))
                        : dbEntities?.FindDbObject(prop.ParentStormType.BindObjectData.ObjectType, x => IsSameDbObject(x.ObjectName, detailTableName) && IsSameDbObject(schemaName, x.SchemaName));

                if (detailTable is null)
                {
                    prop.SetStatus(StormPropertyStatus.DetailTableNotFound, $"Detail table '{detailTableName}' not found");
                    return;
                }

                dbObject = detailTable;
            }

            foreach (var p2 in prop.Details)
            {
                CompareProperty(p2, dbObject, dbEntities, prop.PropertyType.GetFullName(), schemaName);
            }

            var maxStatus = prop.Details.Max(x => x.Status);

            if (maxStatus != StormPropertyStatus.Ok)
            {
                prop.SetStatus(maxStatus, "Expand property to see details");
            }
            else
            {
                prop.SetStatus(StormPropertyStatus.Ok, string.Empty);
            }
            return;
        }

        var column = dbObject.Columns.Find(c => IsSameColumn(prop, c));

        if (column is null)
        {
            prop.SetStatus(StormPropertyStatus.ColumnMissing, $"Property '{prop.PropertyName}' doesn't have corresponding column {prop.GetColumnName()} in table'");
            return;
        }

        if (prop.IsNullable && !column.IsNullable)
        {
            prop.SetStatus(StormPropertyStatus.NullableMismatch, $"Property '{prop.PropertyName}' is nullable, but column '{column.ColumnName}' is not");
            return;
        }

        if (column.IsNullable && !prop.IsNullable)
        {
            prop.SetStatus(StormPropertyStatus.NullableMismatch, $"Property '{prop.PropertyName}' is not nullable, but column '{column.ColumnName}' is");
            return;
        }

        if (column.IsKey && !prop.IsKey)
        {
            prop.SetStatus(StormPropertyStatus.KeyMismatch, $"Property '{prop.PropertyName}' is not marked as key, but column '{column.ColumnName}' is");
            return;
        }

        var eff = prop.BindColumnData;
        if (!eff.DbType.HasValue)
        {
            eff = prop.GetEffectiveBindColumnData();
            if (!eff.DbType.HasValue)
                throw new Exception($"Unknown effective DbType for {prop.PropertyName}");
        }

        if (!CompareColumn(prop, eff, column))
        {
            return;
        }

        //switch (prop.PropertyGenSpec.DbStorageTypeSymbol.CheckDbTypeCompatibility(dbType, prop.BindColumnData.SaveAs))
        //{
        //    case TypeCompatibility.PartiallyCompatible:
        //        prop.SetStatus(StormPropertyStatus.DbTypePartiallyCompatible, $"Property '{prop.PropertyName}' is of type '{prop.PropertyType}{(prop.PropertyGenSpec.Kind == ClassKind.Enum ? $"({prop.PropertyGenSpec.DbStorageTypeSymbol.GetFullName()})" : "")}', but column '{column.ColumnName}' is of type '{column.DataType}'");
        //        return;

        //    case TypeCompatibility.NotCompatible:
        //        prop.SetStatus(StormPropertyStatus.DbTypeNotCompatible, $"Property '{prop.PropertyName}' is of type '{prop.PropertyType}', but column '{column.ColumnName}' is of type '{column.DataType}'");
        //        return;
        //}
    }

    private static bool CompareColumn(StormPropertyDef prop, BindColumnData modelBindData, DbColumnDef column)
    {
        var modelDbType = modelBindData.DbType!.Value;
        var modelNativeDbType = modelBindData.DbType!.Value.ToNativeDbType();
        var nativeDatabaseDbType = column.DataType.ToNativeDbType();

        if (nativeDatabaseDbType == StormNativeDbType.Timestamp && modelDbType == UnifiedDbType.Binary && modelBindData.Size == 8)
        {
            return true;
        }

        if (nativeDatabaseDbType != modelNativeDbType)
        {
            prop.SetStatus(StormPropertyStatus.DbTypeNotCompatible, $"Property '{prop.PropertyName}' is of type '{modelNativeDbType}', but column '{column.ColumnName}' is of type '{nativeDatabaseDbType}'");
            return false;
        }

        if (column.CharacterMaximumLength.HasValue && (prop.BindColumnData.Size ?? (modelDbType.HasMaxSize() ? -1 : 0)) != column.CharacterMaximumLength)
        {
            prop.SetStatus(StormPropertyStatus.SizeMismatch, $"Property '{prop.PropertyName}' has Size={prop.BindColumnData.Size}, but column '{column.ColumnName}' has Size={column.CharacterMaximumLength}");
        }

        if (column.NumericPrecision.HasValue && (prop.BindColumnData.Precision ?? (modelDbType.HasPrecision() ? -1 : 0)) != column.NumericPrecision)
        {
            prop.SetStatus(StormPropertyStatus.PrecisionMismatch, $"Property '{prop.PropertyName}' has Precision={prop.BindColumnData.Size}, but column '{column.ColumnName}' has Precision={column.NumericPrecision}");
        }

        if (column.NumericScale.HasValue && (prop.BindColumnData.Scale ?? (modelDbType.HasScale() ? -1 : 0)) != column.NumericScale)
        {
            prop.SetStatus(StormPropertyStatus.ScaleMismatch, $"Property '{prop.PropertyName}' has Scale={prop.BindColumnData.Scale}, but column '{column.ColumnName}' has Scale={column.NumericScale}");
        }

        return true;
    }

    private static bool IsSameDbObject(string name1, string name2)
    {
        var normalized1 = name1.Trim('[', ']');
        var normalized2 = name2.Trim('[', ']');
        return normalized1.Equals(normalized2, StringComparison.Ordinal);
    }

    private static bool IsSameColumn(StormPropertyDef prop, DbColumnDef dbColumn) => prop.GetColumnName() == dbColumn.ColumnName;
}
