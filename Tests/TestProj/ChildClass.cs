using AltaSoft.Storm.Attributes;

namespace TestProj;

[StormDbObject(UpdateMode = UpdateMode.ChangeTracking)]
public partial record ChildClass : BaseClass
{
    public int ChildId { get; set; }
}
