using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.TestModels.VeryBadNamespace;

namespace AltaSoft.Storm.TestModels
{
    [StormTrackableObject]
    public sealed partial record DerivedTrackableObject : TrackableObject
    {
        public DerivedTrackableObject(string strValue, string? nullStrValue, int intValue, int? nullIntValue, CustomerId customerIdValue) : base(strValue, nullStrValue, intValue, nullIntValue, customerIdValue)
        {
        }
    }
}
