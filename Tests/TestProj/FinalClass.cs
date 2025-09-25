using AltaSoft.Storm.Attributes;

namespace TestProj;

[StormDbObject(UpdateMode = UpdateMode.UpdateAll)]
public sealed partial record FinalClass : ChildClass
{
    public int FinalId { get; set; }
}
