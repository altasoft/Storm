using AltaSoft.DomainPrimitives;

// ReSharper disable once CheckNamespace
namespace AltaSoft.Storm.TestModels.VeryBadNamespace;

public partial class CurrencyId : IDomainValue<string>
{
    public static PrimitiveValidationResult Validate(string value)
    {
        if (value.Length != 3)
            return "Invalid currency";
        return PrimitiveValidationResult.Ok;
    }
}
