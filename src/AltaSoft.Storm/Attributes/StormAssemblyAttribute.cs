using System;

namespace AltaSoft.Storm.Attributes;

/// <summary>
/// Represents an attribute that is applied to assemblies to indicate that they are part of a storm assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class StormAssemblyAttribute : Attribute;
