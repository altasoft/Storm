using System;
using AltaSoft.DomainPrimitives;

namespace AltaSoft.Storm.TestModels.DomainTypes;

/// <summary>
/// DateTime-backed domain primitive (used to verify SaveAs.String conversion of domain values).
/// </summary>
public readonly partial struct EventTime : IDomainValue<DateTime>
{
    /// <inheritdoc />
    public static PrimitiveValidationResult Validate(DateTime value) => PrimitiveValidationResult.Ok;
}
