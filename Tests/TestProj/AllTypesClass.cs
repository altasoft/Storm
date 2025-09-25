using AltaSoft.Storm;
using AltaSoft.Storm.Attributes;

namespace TestProj;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable IDE0049 // Simplify Names

public partial class AllTypesClass
{
    [StormColumn(DbType = UnifiedDbType.Boolean)]
    public bool Bool { get; set; }

    [StormColumn(DbType = UnifiedDbType.UInt8)]
    public byte UInt8 { get; set; }

    [StormColumn(DbType = UnifiedDbType.Int8)]
    public sbyte Int8 { get; set; }

    [StormColumn(DbType = UnifiedDbType.UInt16)]
    public UInt16 UInt16 { get; set; }

    [StormColumn(DbType = UnifiedDbType.Int32)]
    public Int32 Int32 { get; set; }

    [StormColumn(DbType = UnifiedDbType.UInt64)]
    public UInt64 UInt64 { get; set; }

    [StormColumn(DbType = UnifiedDbType.Int64)]
    public Int64 Int64 { get; set; }

    [StormColumn(DbType = UnifiedDbType.Single)]
    public Single Single { get; set; }

    [StormColumn(DbType = UnifiedDbType.Double)]
    public Double Double { get; set; }

    [StormColumn(DbType = UnifiedDbType.Decimal)]
    public Decimal Decimal { get; set; }

    //[StormColumn(DbType = UnifiedDbType.VarNumeric)]
    //public decimal VarNumeric { get; set; }

    [StormColumn(DbType = UnifiedDbType.Guid)]
    public Guid Guid { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 12)]
    public string AnsiString { get; set; }

    [StormColumn(DbType = UnifiedDbType.String, Size = 13)]
    public string String { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiText, Size = 14)]
    public string AnsiText { get; set; }

    [StormColumn(DbType = UnifiedDbType.Text, Size = 15)]
    public string Text { get; set; }

    [StormColumn(DbType = UnifiedDbType.Json, Size = 16)]
    public string Json { get; set; }

    [StormColumn(DbType = UnifiedDbType.Xml, Size = 17)]
    public string Xml { get; set; }

    [StormColumn(ColumnType = ColumnType.PrimaryKey | ColumnType.RowVersion | ColumnType.ConcurrencyCheck)]
    public Timestamp EventId { get; set; }

    [StormColumn(DbType = UnifiedDbType.VarBinary)]
    public byte[] Binary { get; set; }

    [StormColumn(DbType = UnifiedDbType.Blob)]
    public byte[] Blob { get; set; }

    [StormColumn]
    public Dictionary<string, int> DictStrInt { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning restore IDE0049 // Simplify Names
}
