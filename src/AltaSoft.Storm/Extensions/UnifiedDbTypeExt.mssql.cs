#if STORM_MSSQL

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using AltaSoft.Storm.Exceptions;

namespace AltaSoft.Storm.Extensions;

public static partial class UnifiedDbTypeExt
{
    // A mapping dictionary to convert DbType enumeration values to their equivalent StormNativeDbType values.
    // This is used internally to ensure that data types are handled correctly when interacting with Microsoft SQL Server.
#if NET8_0_OR_GREATER
    private static readonly FrozenDictionary<UnifiedDbType, StormNativeDbType?> s_dbTypeSqlTypeMap = new Dictionary<UnifiedDbType, StormNativeDbType?>
#else
    private static readonly IReadOnlyDictionary<UnifiedDbType, StormNativeDbType?> s_dbTypeSqlTypeMap = new Dictionary<UnifiedDbType, StormNativeDbType?>
#endif
    {
        { UnifiedDbType.Boolean, StormNativeDbType.Bit },
        { UnifiedDbType.UInt8, StormNativeDbType.TinyInt },
        { UnifiedDbType.Int8, StormNativeDbType.SmallInt }, // No direct 8-bit signed integer, using SmallInt
        { UnifiedDbType.UInt16, StormNativeDbType.Int }, // No unsigned 16-bit integer, using Int
        { UnifiedDbType.Int16, StormNativeDbType.SmallInt },
        { UnifiedDbType.UInt32, StormNativeDbType.BigInt }, // No unsigned 32-bit integer, using BigInt
        { UnifiedDbType.Int32, StormNativeDbType.Int },
        { UnifiedDbType.UInt64, null }, // No unsigned 64-bit integer, Not supported
        { UnifiedDbType.Int64, StormNativeDbType.BigInt },

        { UnifiedDbType.AnsiString, StormNativeDbType.VarChar },
        { UnifiedDbType.String, StormNativeDbType.NVarChar },
        { UnifiedDbType.AnsiStringFixedLength, StormNativeDbType.Char },
        { UnifiedDbType.StringFixedLength, StormNativeDbType.NChar },
        //{ UnifiedDbType.CompressedString, StormNativeDbType.VarBinary },

        { UnifiedDbType.Currency, StormNativeDbType.Money },
        { UnifiedDbType.Single, StormNativeDbType.Real },
        { UnifiedDbType.Double, StormNativeDbType.Float },
        { UnifiedDbType.Decimal, StormNativeDbType.Decimal },
        //{ UnifiedDbType.VarNumeric, StormNativeDbType.Decimal }, // Using Decimal for VarNumeric

        { UnifiedDbType.SmallDateTime, StormNativeDbType.SmallDateTime },
        { UnifiedDbType.DateTime, StormNativeDbType.DateTime },
        { UnifiedDbType.DateTime2, StormNativeDbType.DateTime2 },
        { UnifiedDbType.DateTimeOffset, StormNativeDbType.DateTimeOffset },
        { UnifiedDbType.Date, StormNativeDbType.Date },
        { UnifiedDbType.Time, StormNativeDbType.Time },

        { UnifiedDbType.Guid, StormNativeDbType.UniqueIdentifier },
        { UnifiedDbType.AnsiXml, StormNativeDbType.Xml },
        { UnifiedDbType.Xml, StormNativeDbType.Xml },
        { UnifiedDbType.AnsiJson, StormNativeDbType.VarChar }, // Storing JSON as varchar
        { UnifiedDbType.Json, StormNativeDbType.NVarChar }, // Storing JSON as nvarchar
        { UnifiedDbType.AnsiText, StormNativeDbType.Text },
        { UnifiedDbType.Text, StormNativeDbType.NText },
        { UnifiedDbType.VarBinary, StormNativeDbType.VarBinary },
        { UnifiedDbType.Binary, StormNativeDbType.Binary },
        { UnifiedDbType.Blob, StormNativeDbType.VarBinary } // Using VarBinary for Blob
    }.ToFrozenDictionary();

    /// <summary>
    /// Converts a SQL data type to its equivalent StormNativeDbType.
    /// </summary>
    /// <param name="dataType">The SQL data type.</param>
    /// <returns>The equivalent StormNativeDbType.</returns>
    public static StormNativeDbType ToNativeDbType(this string dataType)
    {
        return dataType.ToLowerInvariant() switch
        {
            "bigint" => StormNativeDbType.BigInt,
            "binary" => StormNativeDbType.Binary,
            "bit" => StormNativeDbType.Bit,
            "char" => StormNativeDbType.Char,
            "date" => StormNativeDbType.Date,
            "datetime" => StormNativeDbType.DateTime,
            "datetime2" => StormNativeDbType.DateTime2,
            "datetimeoffset" => StormNativeDbType.DateTimeOffset,
            "decimal" => StormNativeDbType.Decimal,
            "float" => StormNativeDbType.Float,
            "geography" => StormNativeDbType.VarChar,
            "geometry" => StormNativeDbType.VarChar,
            "hierarchyid" => StormNativeDbType.VarChar,
            "image" => StormNativeDbType.Image,
            "int" => StormNativeDbType.Int,
            "money" => StormNativeDbType.Money,
            "nchar" => StormNativeDbType.NChar,
            "ntext" => StormNativeDbType.NText,
            "numeric" => StormNativeDbType.Decimal,
            "nvarchar" => StormNativeDbType.NVarChar,
            "real" => StormNativeDbType.Real,
            "smalldatetime" => StormNativeDbType.SmallDateTime,
            "smallint" => StormNativeDbType.SmallInt,
            "smallmoney" => StormNativeDbType.SmallMoney,
            "sql_variant" => StormNativeDbType.VarChar,
            "sysname" => StormNativeDbType.NVarChar,
            "text" => StormNativeDbType.Text,
            "time" => StormNativeDbType.Time,
            "timestamp" => StormNativeDbType.Timestamp,
            "rowversion" => StormNativeDbType.Timestamp,
            "tinyint" => StormNativeDbType.TinyInt,
            "uniqueidentifier" => StormNativeDbType.UniqueIdentifier,
            "varbinary" => StormNativeDbType.VarBinary,
            "varchar" => StormNativeDbType.VarChar,
            "xml" => StormNativeDbType.Xml,
            _ => throw new Exception($"Unknown data type: {dataType}")
        };
    }

    /// <summary>
    /// Converts a <see cref="UnifiedDbType"/> to its corresponding <see cref="StormNativeDbType"/>.
    /// </summary>
    /// <param name="dbType">The <see cref="UnifiedDbType"/> to convert.</param>
    /// <returns>The corresponding <see cref="StormNativeDbType"/>.</returns>
    public static StormNativeDbType ToNativeDbType(this UnifiedDbType dbType)
    {
        if (!s_dbTypeSqlTypeMap.TryGetValue(dbType, out var type) || !type.HasValue)
            throw new StormException($"Unsupported database column type: UnifiedDbType.{dbType}");
        return type.Value;
    }

    /// <summary>
    /// Converts a <see cref="UnifiedDbType"/> to its corresponding SQL Server-specific data type representation, along with optional size, precision, and scale parameters.
    /// </summary>
    /// <param name="dbType">The UnifiedDbType to convert.</param>
    /// <param name="size">The size parameter for the StormNativeDbType.</param>
    /// <param name="precision">The precision parameter for the StormNativeDbType.</param>
    /// <param name="scale">The scale parameter for the StormNativeDbType.</param>
    /// <returns>A string representing the SQL Server data type.</returns>
    public static string ToSqlDbTypeText(this UnifiedDbType dbType, int size, int precision, int scale)
    {
        return dbType.ToNativeDbType().ToSqlDbTypeText(size, precision, scale);
    }

    /// <summary>
    /// Converts a <see cref="StormNativeDbType"/> to its corresponding SQL Server-specific data type representation, along with optional size, precision, and scale parameters.
    /// </summary>
    /// <param name="dbType">The UnifiedDbType to convert.</param>
    /// <param name="size">The size parameter for the SqlDbType.</param>
    /// <param name="precision">The precision parameter for the SqlDbType.</param>
    /// <param name="scale">The scale parameter for the SqlDbType.</param>
    /// <returns>A string representing the SQL Server data type.</returns>
    public static string ToSqlDbTypeText(this StormNativeDbType dbType, int size, int precision, int scale)
    {
        var result = dbType.ToString().ToLowerInvariant();

        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        return dbType switch
        {
            StormNativeDbType.VarChar or StormNativeDbType.NVarChar or StormNativeDbType.Char or StormNativeDbType.NChar
                or StormNativeDbType.Text or StormNativeDbType.NText or StormNativeDbType.VarBinary
                or StormNativeDbType.Binary =>
                $"{result} {(size > 0 ? '(' + size.ToString(CultureInfo.InvariantCulture) + ')' : "(max)")}",
            StormNativeDbType.DateTime2 or StormNativeDbType.Time or StormNativeDbType.DateTimeOffset =>
                $"{result} ({(precision < 0 ? 7 : precision).ToString(CultureInfo.InvariantCulture)})",
            StormNativeDbType.Decimal =>
                $"{result} ({(precision <= 0 ? 18 : precision).ToString(CultureInfo.InvariantCulture)},{scale.ToString(CultureInfo.InvariantCulture)})",
            StormNativeDbType.Float =>
                $"{result} ({(precision <= 0 ? 53 : precision).ToString(CultureInfo.InvariantCulture)})",
            _ => result
        };
    }
}

#endif
