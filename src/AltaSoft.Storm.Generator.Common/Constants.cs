namespace AltaSoft.Storm.Generator.Common;

public static class Constants
{
    public const string StormAttributeNamespace = "AltaSoft.Storm.Attributes";
    public const string StormContextNamespace = "AltaSoft.Storm";

    public const string DomainValueInterfaceFullName = "AltaSoft.DomainPrimitives.IDomainValue<T>";
    public const string SqlRowVersionTypeFullName = "AltaSoft.Storm.SqlRowVersion";
    public const string SqlLogSequenceNumberTypeFullName = "AltaSoft.Storm.SqlLogSequenceNumber";

    public const string TrackingListInterfaceFullName = "AltaSoft.Storm.Interfaces.ITrackingList";
    public const string UnifiedDbTypeFullName = "AltaSoft.Storm.UnifiedDbType";

    public const string StormTrackableObjectAttributeName = "StormTrackableObjectAttribute";

    // Tables/Views/Procedures/Functions
    public const string StormDbObjectAttributeFullName = "AltaSoft.Storm.Attributes.StormDbObjectAttribute`1";
    public const string StormDbObjectAttributeName = "StormDbObjectAttribute";

    public const string StormIndexAttributeName = "StormIndexAttribute";

    public const string StormFunctionAttributeName = "StormFunctionAttribute";
    public const string StormProcedureAttributeName = "StormProcedureAttribute";

    public const string StormDbObjectObjectTypeFullName = "AltaSoft.Storm.Attributes.DbObjectType";
    public const string StormDbObjectUpdateModeFullName = "AltaSoft.Storm.Attributes.UpdateMode";

    public const string StormDbObjectObjectTypePropertyName = "ObjectType";
    public const string StormDbObjectSchemaNamePropertyName = "SchemaName";
    public const string StormDbObjectObjectNamePropertyName = "ObjectName";
    public const string StormDbObjectDisplayNamePropertyName = "DisplayName";
    public const string StormDbObjectUpdateModePropertyName = "UpdateMode";
    public const string StormDbObjectVirtualViewSqlPropertyName = "VirtualViewSql";
    public const string StormDbObjectBulkCopyPropertyName = "BulkInsert";

    public const string StormStringEnumAttributeName = "StormStringEnumAttribute";
    public const string StormStringEnumAttributeToStringName = "ToDbString";
    public const string StormStringEnumAttributeFromStringName = "FromDbString";

    // Columns
    public const string StormColumnAttributeName = "StormColumnAttribute";

    public const string StormColumnSaveAsTypeFullName = "AltaSoft.Storm.Attributes.SaveAs";
    public const string StormColumnColumnTypeFullName = "AltaSoft.Storm.Attributes.ColumnType";

    public const string SaveAsPropertyName = "SaveAs";
    public const string LoadWithFlagsPropertyName = "LoadWithFlags";
    public const string ColumnNamePropertyName = "ColumnName";
    public const string ColumnTypePropertyName = "ColumnType";
    public const string DetailTableNamePropertyName = "DetailTableName";

    // Parameters
    public const string StormParameterAttributeName = "StormParameterAttribute";

    public const string ParameterNamePropertyName = "ParameterName";
    //public const string ParameterDirectionPropertyName = "Direction";

    // Columns and parameters
    public const string DbTypePropertyName = "DbType";

    public const string DbSizePropertyName = "Size";
    public const string DbPrecisionPropertyName = "Precision";
    public const string DbScalePropertyName = "Scale";

    // Other
    public const string AfterLoadMethodName = "AfterLoad";

    // For source generator filtering
    public const string StormTrackableObjectAttributeFullName = "AltaSoft.Storm.Attributes.StormTrackableObjectAttribute";
    public const string StormFunctionAttributeFullName = "AltaSoft.Storm.Attributes.StormFunctionAttribute";
    public const string StormProcedureAttributeFullName = "AltaSoft.Storm.Attributes.StormProcedureAttribute";
}
