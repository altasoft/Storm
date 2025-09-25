using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.TestModels;

namespace AltaSoft.Storm.Tests;

[StormDbObject<TestStormContext>(UpdateMode = UpdateMode.UpdateAll)]
public partial class IlTestClass
{
    public int IlTestProp1 { get; set; }

    public int IlTestProp3 { get; set; }
}
