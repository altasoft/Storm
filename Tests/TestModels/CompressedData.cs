using System;
using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.TestModels.AdventureWorks;

namespace AltaSoft.Storm.TestModels;

[StormDbObject<TestStormContext>(SchemaName = "dbo", DisplayName = "CompressedData")]
public partial record CompressedData
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public Guid Id { get; set; }

    [StormColumn(SaveAs = SaveAs.CompressedString, Size = 50)]
    public string? CompressedStringN { get; set; }

    [StormColumn(SaveAs = SaveAs.CompressedJson, Size = -1)]
    public Product JsonCompressed { get; set; }

    [StormColumn(SaveAs = SaveAs.CompressedXml, Size = -1)]
    public Product XmlCompressed { get; set; }
}
