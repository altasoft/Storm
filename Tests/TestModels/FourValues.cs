using System;
using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

[StormDbObject<TestStormContext>]
internal sealed partial class FourValues
{
    public int I1 { get; set; }
    public int? I2 { get; set; }
    public Int32 I3 { get; set; }
    public Int32? I4 { get; set; }
}
