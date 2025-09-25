using System.Runtime.InteropServices;
using AltaSoft.DomainPrimitives;

// ReSharper disable once CheckNamespace
namespace AltaSoft.Storm.TestModels.VeryBadNamespace;

[StructLayout(LayoutKind.Auto)]
public readonly partial struct CustomerId : IDomainValue<long>
{
    public static PrimitiveValidationResult Validate(long value)
    {
        if (value <= 0)
            return "Invalid Customer Id";
        return PrimitiveValidationResult.Ok;
    }
}
