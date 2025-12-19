using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.TestModels.VeryBadNamespace;

namespace AltaSoft.Storm.TestModels;

[StormDbObject<TestStormContext>(DisplayName = "SqlWhereTestEntity")]
public partial record SqlWhereTestEntity
{
    [StormColumn(SaveAs = SaveAs.String)]
    public RgbColor StringColor { get; set; } = RgbColor.Blue;

    public RgbColor IntColor { get; set; } = RgbColor.Blue;

    public RgbColor? IntColorN { get; set; } = RgbColor.Blue;

    public CustomerId CustomerId { get; set; }

    public CustomerId? CustomerIdN { get; set; }

    public int IntValue { get; set; }

    public int? IntValueN { get; set; }

    public string? StringName { get; set; }

    public string? StringNameN { get; set; }

    public CurrencyId Ccy { get; set; }

    public CurrencyId? CcyN { get; set; }
}

