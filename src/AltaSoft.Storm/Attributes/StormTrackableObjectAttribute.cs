using System;

namespace AltaSoft.Storm.Attributes;

/// <summary>
/// Specifies that the attributed class or struct is a trackable object for storm tracking.
/// </summary>
/// <returns>
/// This attribute can be applied to classes or structs to mark them as trackable objects for storm tracking.
/// </returns>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class StormTrackableObjectAttribute : Attribute;
