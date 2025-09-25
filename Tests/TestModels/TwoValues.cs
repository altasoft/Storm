using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

[StormDbObject<TestStormContext>]
public partial class TwoValues
{
    public int I1 { get; set; }
    public int? I2 { get; set; }

    public int NoSetter
    {
        get { return 0; }
    }

    public int NoGetter
    {
        set { _ = value; }
    }
}
