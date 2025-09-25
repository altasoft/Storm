using System;
using System.Linq.Expressions;
using System.Reflection;

namespace AltaSoft.Storm.Helpers;

/// <summary>
/// This class is responsible for reducing predicate expressions by simplifying and transforming them.
/// It inherits from the ExpressionVisitor class.
/// </summary>
/// <remarks>
/// The PredicateExpressionReducer class provides methods for reducing unary and binary expressions, as well as member expressions.
/// It also includes helper methods for checking if a member expression represents a nullable type with a "HasValue" property.
/// The class is implemented as a singleton with a static instance property.
/// </remarks>
internal sealed class PredicateExpressionReducer : ExpressionVisitor
{
    /// <summary>
    /// Represents a static instance of the PredicateExpressionReducer class.
    /// </summary>
    public static readonly PredicateExpressionReducer Instance = new();

    /// <inheritdoc/>
    protected override Expression VisitUnary(UnaryExpression node)
    {
        // Check if the unary operation is a 'Not' (logical negation).
        if (node.NodeType == ExpressionType.Not)
        {
            // If it is, process the operand of the 'Not' expression with VisitNotOperand.
            // If VisitNotOperand returns null, defer to the base class's implementation.
            return VisitNotOperand(node.Operand) ?? base.VisitUnary(node);
        }

        // Check if the unary operation is a 'Convert' and the operand's type is a generic type.
        // This is typically used for nullable type conversions.
        if (node is { NodeType: ExpressionType.Convert, Operand.Type: { IsGenericType: true } declaringType }
            && declaringType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            // If it is, directly visit the operand, bypassing the conversion.
            // This is likely to handle unwrapping of nullable types without an explicit conversion.
            return Visit(node.Operand);
        }

        // For all other types of unary operations, defer to the base class's implementation.
        return base.VisitUnary(node);
    }

    private Expression? VisitNotOperand(Expression node) => node switch
    {
        // Handle a unary expression with a NOT operation, visit its operand directly.
        UnaryExpression { NodeType: ExpressionType.Not, Operand: var operand } => Visit(operand),

        // If the expression is a member expression that represents checking the 'HasValue' property of a nullable type,
        // return an expression that checks if the member is not null.
        MemberExpression { Expression: MemberExpression { Expression: ParameterExpression } ime } me
            when IsNullableHasValue(me) => Expression.NotEqual(ime, Expression.Constant(null)),

        // If the expression is a simple member expression (direct property of a parameter), 
        // return an expression that checks if the member is equal to false.
        MemberExpression { Expression: ParameterExpression } me => Expression.Equal(me, Expression.Constant(false)),

        // If the member expression represents checking 'HasValue' of a nullable type and its parent expression is constant,
        // return a constant false expression.
        MemberExpression m when IsNullableHasValue(m) && Visit(m.Expression) is ConstantExpression =>
            Expression.Constant(false),

        // For other cases, visit the node and then apply further processing based on its type.
        _ => Visit(node) switch
        {
            // For a constant boolean expression, return the negation of its value.
            ConstantExpression(bool c) => Expression.Constant(!c),

            // For binary expressions, reverse their logical operation.
            BinaryExpression { Left: var l, NodeType: ExpressionType.Equal, Right: var r } => Expression.NotEqual(l, r),
            BinaryExpression { Left: var l, NodeType: ExpressionType.NotEqual, Right: var r } => Expression.Equal(l, r),
            BinaryExpression { Left: var l, NodeType: ExpressionType.LessThan, Right: var r } => Expression.GreaterThanOrEqual(l, r),
            BinaryExpression { Left: var l, NodeType: ExpressionType.GreaterThan, Right: var r } => Expression.LessThanOrEqual(l, r),

            // For other expression types, apply a NOT operation.
            { } n => Expression.Not(n),

            // If none of the above conditions are met, return null.
            _ => null
        }
    };

    /// <inheritdoc/>
    protected override Expression VisitBinary(BinaryExpression node) => node.NodeType switch
    {
        // If the binary operation is an 'OrElse' (logical OR), process it with the VisitOrElse method.
        // If VisitOrElse returns a non-null expression, return that expression.
        ExpressionType.OrElse when VisitOrElse(node) is { } e => e,

        // If the binary operation is an 'AndAlso' (logical AND), process it with the VisitAndAlso method.
        // If VisitAndAlso returns a non-null expression, return that expression.
        ExpressionType.AndAlso when VisitAndAlso(node) is { } e => e,

        // If the binary operation is an equality check, process it with the VisitEqual method.
        ExpressionType.Equal => VisitEqual(node),

        // If the binary operation is a non-equality check, process it with the VisitNotEqual method.
        ExpressionType.NotEqual => VisitNotEqual(node),

        // For all other types of binary operations, defer to the base class's implementation of VisitBinary.
        _ => base.VisitBinary(node)
    };

    private Expression? VisitOrElse(BinaryExpression node) => Visit(node.Left) switch
    {
        // If the left operand is a constant expression that evaluates to true, return a constant true.
        // Since true OR anything is true, there's no need to evaluate the right operand.
        ConstantExpression(true) => Expression.Constant(true),

        // If the left operand is a constant expression that evaluates to false, return the result of visiting the right operand.
        // This is because false OR right_operand is equivalent to right_operand.
        ConstantExpression(false) => Visit(node.Right),

        // If the left operand is not a constant expression, evaluate the right operand.
        { } left => Visit(node.Right) switch
        {
            // If the right operand is a constant expression that evaluates to true, return a constant true.
            // Since anything OR true is true, the overall expression is true.
            ConstantExpression(true) => Expression.Constant(true),

            // If the right operand is a constant expression that evaluates to false, return the left operand.
            // This is because left_operand OR false is equivalent to left_operand.
            ConstantExpression(false) => left,

            // If both left and right operands are not constant expressions, return an OrElse expression combining them.
            { } right => Expression.OrElse(left, right),

            // If none of the above conditions are met for the right operand, return null.
            _ => null
        },

        // If none of the above conditions are met for the left operand, return null.
        _ => null
    };

    private Expression? VisitAndAlso(BinaryExpression node) => Visit(node.Left) switch
    {
        // If the left operand is a constant expression that evaluates to false, return a constant false.
        // Since false AND anything is false, there's no need to evaluate the right operand.
        ConstantExpression(false) => Expression.Constant(false),

        // If the left operand is a constant expression that evaluates to true, return the result of visiting the right operand.
        // This is because true AND right_operand is equivalent to right_operand.
        ConstantExpression(true) => Visit(node.Right),

        // If the left operand is not a constant expression, evaluate the right operand.
        { } left => Visit(node.Right) switch
        {
            // If the right operand is a constant expression that evaluates to false, return a constant false.
            // Since anything AND false is also false, the overall expression is false.
            ConstantExpression(false) => Expression.Constant(false),

            // If the right operand is a constant expression that evaluates to true, return the left operand.
            // This is because left_operand AND true is equivalent to left_operand.
            ConstantExpression(true) => left,

            // If both left and right operands are not constant expressions, return an AndAlso expression combining them.
            { } right => Expression.AndAlso(left, right),

            // If none of the above conditions are met for the right operand, return null.
            _ => null,
        },

        // If none of the above conditions are met for the left operand, return null.
        _ => null
    };

    private Expression VisitEqual(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        return (left, right) switch
        {
            // When both sides are constants, return a constant expression representing the equality of their values.
            (ConstantExpression(var l), ConstantExpression(var r)) => Expression.Constant(l == r),

            // When the left side is a constant and the right is not, return an expression checking equality between them.
            (ConstantExpression l, var r) => Expression.Equal(r, l),

            // When the left side is not a constant, the right is a constant, and their types are different,
            // but the non-nullable type of left is the same as the type of right, convert right to left's type and check equality.
            (var l, ConstantExpression r) when l.Type != r.Type && Nullable.GetUnderlyingType(l.Type) == r.Type => Expression.Equal(Expression.Convert(r, l.Type), l),

            // When both sides are of the same type, return an expression checking equality between them.
            (var l, ConstantExpression r) when l.Type == r.Type => Expression.Equal(l, r),

            // When left is not a constant, right is a member expression, and their types are different,
            // but the non-nullable type of left is the same as the type of right, convert right to left's type and check equality.
            (var l, MemberExpression r) when l.Type != r.Type && Nullable.GetUnderlyingType(l.Type) == r.Type => Expression.Equal(Expression.Convert(r, l.Type), l),

            // When both sides are of the same type, with the right side being a member expression, return an expression checking equality.
            (var l, MemberExpression r) when l.Type == r.Type => Expression.Equal(l, r),

            // Special case for binary expressions: if the right side is a constant true, return the left side as is.
            (BinaryExpression { NodeType: ExpressionType.Equal } l, ConstantExpression(true)) => l,

            // Special case for binary expressions: if the right side is a constant false, return an expression that checks inequality.
            (BinaryExpression { NodeType: ExpressionType.Equal } l, ConstantExpression(false)) => Expression.NotEqual(l.Left, l.Right),

            // Default case: for any other combinations, return an expression checking equality.
            var (l, r) => Expression.Equal(l, r)
        };
    }

    private Expression VisitNotEqual(BinaryExpression node) => (Visit(node.Left), Visit(node.Right)) switch
    {
        // When both sides are constants, return a constant expression representing the inequality of their values.
        (ConstantExpression(var l), ConstantExpression(var r)) => Expression.Constant(l != r),

        // When the left side is a constant and the right is not, return an expression checking inequality between them.
        (ConstantExpression l, var r) => Expression.NotEqual(r, l),

        // When the left side is not a constant, the right is a constant, and their types are different,
        // but the non-nullable type of left is the same as the type of right, convert right to left's type and check inequality.
        (var l, ConstantExpression r) when l.Type != r.Type && Nullable.GetUnderlyingType(l.Type) == r.Type =>
            Expression.NotEqual(Expression.Convert(r, l.Type), l),

        // When both sides are of the same type, return an expression checking inequality between them.
        (var l, ConstantExpression r) when l.Type == r.Type => Expression.NotEqual(l, r),

        // When left is not a constant, right is a member expression, and their types are different,
        // but the non-nullable type of left is the same as the type of right, convert right to left's type and check inequality.
        (var l, MemberExpression r) when l.Type != r.Type && Nullable.GetUnderlyingType(l.Type) == r.Type =>
            Expression.NotEqual(Expression.Convert(r, l.Type), l),

        // When both sides are of the same type, with the right side being a member expression, return an expression checking inequality.
        (var l, MemberExpression r) when l.Type == r.Type => Expression.NotEqual(l, r),

        // Special case for binary expressions: if the right side is a constant false, return the left side as is.
        (BinaryExpression { NodeType: ExpressionType.Equal } l, ConstantExpression(false)) => l,

        // Special case for binary expressions: if the right side is a constant true, return an expression that checks inequality.
        (BinaryExpression { NodeType: ExpressionType.Equal } l, ConstantExpression(true)) => Expression.NotEqual(l.Left, l.Right),

        // Default case: for any other combinations, return an expression checking inequality.
        var (l, r) => Expression.NotEqual(l, r)
    };

    /// <inheritdoc/>
    protected override Expression VisitMember(MemberExpression node) => node switch
    {
        // If the member is a direct property or field of a parameter (e.g., x.MyProperty) and its type is bool,
        // create an expression to check if it's equal to true.
        { Expression: ParameterExpression } when node.Type == typeof(bool) => Expression.Equal(node, Expression.Constant(true)),

        // If the member is a nested property, and it represents the 'HasValue' property of a Nullable type,
        // create an expression to check if the parent member is null.
        { Expression: MemberExpression { Expression: ParameterExpression } ime } when IsNullableHasValue(node) => Expression.Equal(ime, Expression.Constant(null)),

        // If the member is a nested property (e.g., x.MyProperty.MySubProperty), throw an exception as it's not supported.
        { Expression: MemberExpression { Expression: ParameterExpression } } => throw new NotSupportedException("Nested properties not supported"),

        // If the member is a static field, return its value as a constant expression.
        { Member: FieldInfo { IsStatic: true } fi } => Expression.Constant(fi.GetValue(null)),

        // If the member is a static property, return its value as a constant expression.
        { Member: PropertyInfo { GetMethod.IsStatic: true } pi } => Expression.Constant(pi.GetValue(null)),

        // If the member represents the 'HasValue' property of a Nullable type and its parent expression is a constant,
        // return a constant expression indicating if the parent's value is not null.
        { Expression: { } ie } when IsNullableHasValue(node) && Visit(ie) is ConstantExpression { Value: var c } => Expression.Constant(c is { }),

        // If the member is a field and its parent expression is a constant, return the field's value from that parent.
        { Member: FieldInfo fi, Expression: var e } when Visit(e) is ConstantExpression { Value: var c } => Expression.Constant(fi.GetValue(c)),

        // If the member is a property and its parent expression is a constant, return the property's value from that parent.
        { Member: PropertyInfo pi, Expression: var e } when Visit(e) is ConstantExpression { Value: var c } => Expression.Constant(pi.GetValue(c)),

        // For all other cases, defer to the base method's handling.
        _ => base.VisitMember(node)
    };

    /// <summary>
    /// Determines whether the member expression represents a nullable type with a "HasValue" property.
    /// </summary>
    /// <param name="input">The member expression to check.</param>
    /// <returns>True if the member expression represents a nullable type with a "HasValue" property; otherwise, false.</returns>
    private static bool IsNullableHasValue(MemberExpression input)
    {
        return
            input is { Member: { Name: "HasValue", DeclaringType: { IsGenericType: true } declaringType } } &&
            declaringType.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
}
