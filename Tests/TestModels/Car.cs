using System;
using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

[StormDbObject<TestStormContext>(SchemaName = "dbo", DisplayName = "Cars")]
[StormDbObject<TestStormContext>(SchemaName = "dbo", ObjectName = "CarsView", ObjectType = DbObjectType.View)]
public partial record Car
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public Guid CarId { get; set; }

    [StormColumn(DbType = UnifiedDbType.Int32)]
    public int Year { get; set; }
    public string Model { get; set; }

    [StormColumn(SaveAs = SaveAs.String)]
    public RgbColor Color { get; set; } = RgbColor.Blue;

    [StormColumn(SaveAs = SaveAs.CompressedString, Size = 50)]
    public RgbColor CompressedColor { get; set; } = RgbColor.Blue;

    [StormColumn(SaveAs = SaveAs.String, Size = 50)]
    public BwColor Color2 { get; set; } = BwColor.Black;

    [StormColumn(SaveAs = SaveAs.CompressedString, Size = 50)]
    public BwColor CompressedColor2 { get; set; } = BwColor.Black;

    [StormColumn(SaveAs = SaveAs.String, Size = 50)]
    public string? StringN { get; set; }

    [StormColumn(SaveAs = SaveAs.CompressedString, Size = 50)]
    public string? CompressedStringN { get; set; }

    [StormColumn(SaveAs = SaveAs.String, Size = 50)]
    public string String { get; set; } = "DoNotCompressMe";

    [StormColumn(SaveAs = SaveAs.CompressedString, Size = 50)]
    public string CompressedString { get; set; } = "CompressMe";

    [StormColumn(SaveAs = SaveAs.Json, DbType = UnifiedDbType.AnsiJson, Size = 50)]
    public Post? JsonPost { get; set; }

    //[StormColumn(DbType = UnifiedDbType.VarBinary, Size = 2000)]
    //public byte[]? Picture1 { get; set; }

    //[StormColumn(DbType = UnifiedDbType.Binary, Size = 2000)]
    //public byte[]? Picture2 { get; set; }
}
