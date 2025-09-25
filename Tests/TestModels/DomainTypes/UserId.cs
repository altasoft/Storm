using AltaSoft.DomainPrimitives;

namespace AltaSoft.Storm.TestModels.DomainTypes;

public readonly partial struct UserId: IDomainValue<int>
{
    /// <inheritdoc />
    public static PrimitiveValidationResult Validate(int value) => PrimitiveValidationResult.Ok;
}
