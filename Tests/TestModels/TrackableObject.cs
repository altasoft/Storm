using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.TestModels.VeryBadNamespace;

namespace AltaSoft.Storm.TestModels;

[StormTrackableObject]
public abstract partial record TrackableObject
{
    public string StrValue { get; set; }
    public string? NullStrValue { get; set; }
    public int IntValue { get; set; }
    public int? NullIntValue { get; set; }
    public CustomerId CustomerIdValue { get; set; }

    protected TrackableObject(string strValue, string? nullStrValue, int intValue, int? nullIntValue, CustomerId customerIdValue)
    {
        StrValue = strValue;
        NullStrValue = nullStrValue;
        IntValue = intValue;
        NullIntValue = nullIntValue;
        CustomerIdValue = customerIdValue;
    }
}
