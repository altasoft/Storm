namespace AltaSoft.Storm.Weaver;

/// <summary>
/// Constants class containing the full names of some classes and attributes.
/// </summary>
internal static class Constants
{
    public const string StormDbObjectAttributeNamespace = "AltaSoft.Storm.Attributes";

    public const string StormDbObjectAttributeName = "StormDbObjectAttribute`1";

    public const string StormTrackableObjectAttributeName = "StormTrackableObjectAttribute";

    public const string StormDbObjectAttributeUpdateModePropertyName = "UpdateMode";
    public const int StormDbObjectAttributeUpdateModeChangeTrackingValue = 0; // UpdateMode.ChangeTracking

    public const string StormColumnAttributeName = "StormColumnAttribute";
    public const string StormColumnAttributeSaveAs = "SaveAs";
}
