using System;

namespace AltaSoft.Storm.Helpers;

/// <summary>
/// Provides extension methods for strings representing SQL data types. 
/// This class contains methods for converting SQL data type names into their equivalent .NET data type names 
/// and into a custom UnifiedDbType enumeration. 
/// It is designed to facilitate the mapping between SQL data types and .NET or custom data types, 
/// especially useful in scenarios like database schema introspection or data type conversion in Storm.
/// </summary>
internal static class StringExt
{
    /// <summary>
    /// Converts a SQL data type to its equivalent .NET data type.
    /// </summary>
    /// <param name="dataType">The SQL data type.</param>
    /// <returns>The equivalent .NET data type as a string.</returns>
    /// <exception cref="NotImplementedException">Thrown when an unknown data type is encountered.</exception>
    public static string ToDotNetType(this string dataType)
    {
        return dataType switch
        {
            "bigint" => "long",
            "binary" => "byte[]",
            "bit" => "bool",
            "char" => "string",
            "date" => "DateTime",
            "datetime" => "DateTime",
            "datetime2" => "DateTime",
            "datetimeoffset" => "DateTimeOffset",
            "decimal" => "decimal",
            "float" => "double",
            "geography" => "string",
            "geometry" => "string",
            "hierarchyid" => "string",
            "image" => "byte[]",
            "int" => "int",
            "money" => "decimal",
            "nchar" => "string",
            "ntext" => "string",
            "numeric" => "decimal",
            "nvarchar" => "string",
            "real" => "float",
            "smalldatetime" => "DateTime",
            "smallint" => "short",
            "smallmoney" => "decimal",
            "sql_variant" => "string",
            "sysname" => "string",
            "text" => "string",
            "time" => "TimeSpan",
            "timestamp" => "SqlRowVersion",
            "rowversion" => "SqlRowVersion",
            "tinyint" => "byte",
            "uniqueidentifier" => "Guid",
            "varbinary" => "byte[]",
            "varchar" => "string",
            "xml" => "string",
            _ => throw new NotImplementedException($"Unknown data type: {dataType}")
        };
    }

    /// <summary>
    /// Converts a SQL data type to its equivalent UnifiedDbType.
    /// </summary>
    /// <param name="dataType">The SQL data type.</param>
    /// <returns>The equivalent UnifiedDbType.</returns>
    /// <exception cref="NotImplementedException">Thrown when an unknown data type is encountered.</exception>
    public static UnifiedDbType ToUnifiedDbType(this string dataType)
    {
        return dataType.ToLowerInvariant() switch
        {
            "bigint" => UnifiedDbType.Int64,
            "binary" => UnifiedDbType.Binary,
            "bit" => UnifiedDbType.Boolean,
            "char" => UnifiedDbType.AnsiStringFixedLength,
            "date" => UnifiedDbType.Date,
            "datetime" => UnifiedDbType.DateTime,
            "datetime2" => UnifiedDbType.DateTime2,
            "datetimeoffset" => UnifiedDbType.DateTimeOffset,
            "decimal" => UnifiedDbType.Decimal,
            "float" => UnifiedDbType.Double,
            "geography" => UnifiedDbType.String,
            "geometry" => UnifiedDbType.String,
            "hierarchyid" => UnifiedDbType.String,
            "image" => UnifiedDbType.Blob,
            "int" => UnifiedDbType.Int32,
            "money" => UnifiedDbType.Decimal,
            "nchar" => UnifiedDbType.StringFixedLength,
            "ntext" => UnifiedDbType.Text,
            "numeric" => UnifiedDbType.Decimal,
            "nvarchar" => UnifiedDbType.String,
            "real" => UnifiedDbType.Single,
            "smalldatetime" => UnifiedDbType.SmallDateTime,
            "smallint" => UnifiedDbType.Int16,
            "smallmoney" => UnifiedDbType.Decimal,
            "sql_variant" => UnifiedDbType.String,
            "sysname" => UnifiedDbType.String,
            "text" => UnifiedDbType.AnsiText,
            "time" => UnifiedDbType.Time,
            "timestamp" => UnifiedDbType.Binary,
            "rowversion" => UnifiedDbType.Binary,
            "tinyint" => UnifiedDbType.UInt8,
            "uniqueidentifier" => UnifiedDbType.Guid,
            "varbinary" => UnifiedDbType.VarBinary,
            "varchar" => UnifiedDbType.AnsiString,
            "xml" => UnifiedDbType.Xml,
            _ => throw new NotImplementedException($"Unknown data type: {dataType}")
        };
    }
}
