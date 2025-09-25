using System;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AltaSoft.Storm.Models;

internal sealed class DbColumnDef
{
    public int Id { get; }
    public string ColumnName { get; set; }
    public bool IsNullable { get; set; }
    public string DataType { get; set; }
    public int? CharacterMaximumLength { get; set; }
    public byte? NumericPrecision { get; set; }
    public byte? NumericScale { get; set; }
    public bool IsKey { get; set; }
    public bool IsAutoIncrement { get; set; }
    public string? DefaultValue { get; set; }

    public DbObjectDef ParentDbObject { get; internal set; } = default!;

    public string DataTypeFullName => GetDbTypeDisplayString();
    public string NullableText => IsNullable ? "NULL" : "NOT NULL";

    public ImageMoniker IsKeyImage => IsKey ? KnownMonikers.Key : KnownMonikers.Blank;

    private string GetDbTypeDisplayString()
    {
        var dataType = DataType switch
        {
            "xml" => DataType,
            "datetime2" or "time" or "datetimeoffset" => $"{DataType} ({NumericPrecision ?? 7})",
            "decimal" or "numeric" => $"{DataType} ({NumericPrecision ?? 18}, {NumericScale ?? 0})",
            "float" => $"{DataType} ({NumericPrecision ?? 53})",
            _ =>
                CharacterMaximumLength.HasValue
                    ? $"{DataType} ({(CharacterMaximumLength == -1 ? "Max" : CharacterMaximumLength.Value.ToString(CultureInfo.InvariantCulture))})"
                    : DataType
        };

        if (IsAutoIncrement)
            dataType += " (AutoInc)";
        return dataType;
    }

    public DbColumnDef(SqlDataReader reader)
    {
        Id = (int)reader["object_id"];
        ColumnName = (string)reader["name"];
        IsNullable = (bool)reader["is_nullable"];
        DataType = (string)reader["data_type"];
        CharacterMaximumLength = Read<int>("max_length");
        NumericPrecision = Read<byte>("precision");
        NumericScale = Read<byte>("scale");
        IsKey = (int)reader["is_part_of_pk"] != 0;
        IsAutoIncrement = (bool)reader["is_identity"];
        DefaultValue = reader["default_value"] == DBNull.Value ? null : (string)reader["default_value"];

        // Analyse
        var isCharOrBinary = DataType is "char" or "varchar" or "nchar" or "nvarchar" or "binary" or "varbinary";
        if (isCharOrBinary)
        {
            if (CharacterMaximumLength == 0)
                CharacterMaximumLength = -1;
            return;
        }

        CharacterMaximumLength = null;

        var isNumeric = DataType is "decimal" or "numeric";
        var isDateTime2 = DataType is "datetime2" or "datetimeoffset" or "time";

        if (!(isDateTime2 || isNumeric))
        {
            NumericPrecision = null;
            NumericScale = null;
            return;
        }

        if (isDateTime2)
        {
            NumericScale = null;
            return;
        }

        return;

        T? Read<T>(string name) where T : struct
        {
            var o = reader[name];
            return o == DBNull.Value ? null : (T)o;
        }
    }

    // For Dummy
    private DbColumnDef()
    {
        ColumnName = "Loading...";
        DataType = string.Empty;
    }

    public static DbColumnDef CreateDummy() => new();
}
