using System;
using System.Linq.Expressions;

namespace AltaSoft.Storm.Extensions;

/// <summary>
/// Contains extension methods for working with expressions.
/// </summary>
internal static class ExpressionExt
{
    /// <summary>
    /// Retrieves the name of the property from the given expression.
    /// </summary>
    /// <typeparam name="T">The type of the object containing the property</typeparam>
    /// <typeparam name="TResult">The type of the property</typeparam>
    /// <param name="expression">The expression representing the property access</param>
    /// <returns>The name of the property accessed in the expression</returns>
    public static string GetPropertyNameFromExpression<T, TResult>(this Expression<Func<T, TResult>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
            return memberExpression.Member.Name;

        throw new ArgumentException("Expression is not a member access", nameof(expression));
    }
}
