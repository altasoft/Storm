using System.Linq.Expressions;

namespace AltaSoft.Storm.Helpers;

/// <summary>
/// Extension class for expressions that provides a Deconstruct method for ConstantExpression.
/// </summary>
internal static class ExpressionExt
{
    public static void Deconstruct(this ConstantExpression self, out object? value) => value = self.Value;
}
