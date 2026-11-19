using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AltaSoft.Storm.Helpers;

/// <summary>
/// Reduces predicate expressions by simplifying and transforming expression tree constructs.
/// This visitor rewrites certain patterns (nullable checks, boolean member access, bitwise operations, constant folding)
/// to simpler forms that are easier to translate to SQL.
/// </summary>
internal sealed class PredicateExpressionReducer : ExpressionVisitor
{
    /// <summary>
    /// Singleton instance of the reducer to avoid allocating multiple visitors.
    /// </summary>
    public static readonly PredicateExpressionReducer Instance = new();

    /// <summary>
    /// Visits a unary expression and attempts to reduce it. Supports logical negation and preserves
    /// conversions for nullable operand types.
    /// </summary>
    /// <param name="node">The unary expression to visit.</param>
    /// <returns>The reduced expression or the original visited expression.</returns>
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
        if (node is { NodeType: ExpressionType.Convert, Operand.Type: { IsGenericType: true } declaringType } && IsNullableT(declaringType))
        {
            // Previously we unwrapped the conversion which caused invalid binary operations
            // for cases like '(int)x.NullableEnum'. Preserve the conversion by visiting the operand
            // and then applying the original conversion to the visited result.
            var visited = Visit(node.Operand);
            return Expression.Convert(visited, node.Type);
        }

        // For all other types of unary operations, defer to the base class's implementation.
        return base.VisitUnary(node);
    }

    /// <summary>
    /// Helper used when visiting the operand of a logical 'not' expression. Performs common rewrites
    /// such as inverting comparisons and simplifying nullable checks.
    /// </summary>
    /// <param name="node">Operand expression of a logical NOT.</param>
    /// <returns>Transformed expression or null if unable to reduce.</returns>
    private Expression? VisitNotOperand(Expression node) => node switch
    {
        // Handle a unary expression with a NOT operation, visit its operand directly.
        UnaryExpression { NodeType: ExpressionType.Not, Operand: var operand } => Visit(operand),

        // !x.Nullable.HasValue => x.Nullable IS NULL
        MemberExpression { Expression: MemberExpression { Expression: ParameterExpression } ime } me when IsNullableHasValue(me) => Expression.Equal(ime, Expression.Constant(null)),

        // !x.BoolColumn => x.BoolColumn = false
        MemberExpression { Expression: ParameterExpression } me when me.Type == typeof(bool) => Expression.Equal(me, Expression.Constant(false)),

        // !captured.HasValue => evaluate the captured value
        MemberExpression m when IsNullableHasValue(m) && Visit(m.Expression) is ConstantExpression { Value: var c } => Expression.Constant(c is null),

        // For other cases, visit the node and negate the result.
        _ => Visit(node) is { } visited ? Negate(visited) : null
    };

    /// <summary>
    /// Returns the logical negation of an already reduced boolean predicate.
    /// </summary>
    /// <param name="node">The reduced predicate.</param>
    /// <returns>The negated predicate.</returns>
    private static Expression Negate(Expression node) => node switch
    {
        // For a constant boolean expression, return the negation of its value.
        ConstantExpression { Value: bool c } => Expression.Constant(!c),

        // A raw boolean column: column = false
        MemberExpression { Expression: ParameterExpression } me when me.Type == typeof(bool) => Expression.Equal(me, Expression.Constant(false)),

        // Boolean value compared to a boolean constant (e.g. x.Flag = true, x.FlagN.Value = true): flip the constant.
        BinaryExpression { NodeType: ExpressionType.Equal, Left: var l, Right: ConstantExpression { Value: bool b } } when l.Type == typeof(bool) => Expression.Equal(l, Expression.Constant(!b)),

        // For binary expressions, reverse their logical operation.
        BinaryExpression { Left: var l, NodeType: ExpressionType.Equal, Right: var r } => Expression.NotEqual(l, r),
        BinaryExpression { Left: var l, NodeType: ExpressionType.NotEqual, Right: var r } => Expression.Equal(l, r),
        BinaryExpression { Left: var l, NodeType: ExpressionType.LessThan, Right: var r } => Expression.GreaterThanOrEqual(l, r),
        BinaryExpression { Left: var l, NodeType: ExpressionType.GreaterThan, Right: var r } => Expression.LessThanOrEqual(l, r),
        BinaryExpression { Left: var l, NodeType: ExpressionType.LessThanOrEqual, Right: var r } => Expression.GreaterThan(l, r),
        BinaryExpression { Left: var l, NodeType: ExpressionType.GreaterThanOrEqual, Right: var r } => Expression.LessThan(l, r),

        // For other expression types, apply a NOT operation.
        _ => Expression.Not(node)
    };

    /// <summary>
    /// Visits a binary expression and attempts to reduce it. Special handling is provided for
    /// logical conjunction/disjunction, equality/inequality and bitwise operations.
    /// </summary>
    /// <param name="node">The binary expression to visit.</param>
    /// <returns>The reduced expression.</returns>
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

        // For bitwise operations (&, |, ^) ensure operand types are compatible (convert nullable operands to the other side's type)
        ExpressionType.And or ExpressionType.Or or ExpressionType.ExclusiveOr => HandleBitwise(node),

        // For all other types of binary operations, defer to the base class's implementation of VisitBinary.
        _ => base.VisitBinary(node)
    };

    /// <summary>
    /// Ensures bitwise operands are type-compatible by converting nullable operands to the non-nullable counterpart when needed.
    /// This prevents binary operator type errors when one operand remains Nullable&lt;T&gt; while the other is T.
    /// Only applies conversions for integral and enum types, not for boolean logical operations.
    /// </summary>
    /// <param name="node">The original binary expression.</param>
    /// <returns>A rewritten binary expression with compatible operand types.</returns>
    private static BinaryExpression HandleBitwise(BinaryExpression node)
    {
        var left = Instance.Visit(node.Left);
        var right = Instance.Visit(node.Right);

        // Only apply type conversion for integral/enum types, not for boolean logical operations
        // If one side is nullable (Nullable<>) and the other is a non-nullable value type, convert the nullable side to the other's type
        if (left is { Type: var lt } && right is { Type: var rt } && lt != rt)
        {
            if (IsNullableT(lt) && IsNonNullableValueType(rt))
            {
                left = Expression.Convert(left, rt);
            }
            else if (IsNullableT(rt) && IsNonNullableValueType(lt))
            {
                right = Expression.Convert(right, lt);
            }
        }

        return node.NodeType switch
        {
            ExpressionType.And => Expression.MakeBinary(ExpressionType.And, left, right),
            ExpressionType.Or => Expression.MakeBinary(ExpressionType.Or, left, right),
            ExpressionType.ExclusiveOr => Expression.MakeBinary(ExpressionType.ExclusiveOr, left, right),
            _ => throw new NotSupportedException("Invalid bitwise operator")
        };
    }

    /// <summary>
    /// Reduces logical OR expressions with constant folding when possible.
    /// </summary>
    /// <param name="node">The OR expression to reduce.</param>
    /// <returns>The reduced expression or null if no reduction is possible.</returns>
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

            // If none of the above conditions are met, return null.
            _ => null
        },

        // If none of the above conditions are met for the left operand, return null.
        _ => null
    };

    /// <summary>
    /// Reduces logical AND expressions with constant folding when possible.
    /// </summary>
    /// <param name="node">The AND expression to reduce.</param>
    /// <returns>The reduced expression or null if no reduction is possible.</returns>
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

    /// <summary>
    /// Reduces equality expressions with special handling for nullable conversions, boolean predicates and constant folding.
    /// </summary>
    /// <param name="node">The equality expression to reduce.</param>
    /// <returns>The reduced expression.</returns>
    private Expression VisitEqual(BinaryExpression node) => VisitEquality(node, isNotEqual: false);

    /// <summary>
    /// Reduces inequality expressions with special handling for nullable conversions, boolean predicates and constant folding.
    /// </summary>
    /// <param name="node">The inequality expression to reduce.</param>
    /// <returns>The reduced expression.</returns>
    private Expression VisitNotEqual(BinaryExpression node) => VisitEquality(node, isNotEqual: true);

    /// <summary>
    /// Shared implementation of <see cref="VisitEqual"/> and <see cref="VisitNotEqual"/>.
    /// </summary>
    /// <param name="node">The equality or inequality expression to reduce.</param>
    /// <param name="isNotEqual"><c>true</c> for inequality; <c>false</c> for equality.</param>
    /// <returns>The reduced expression.</returns>
    private Expression VisitEquality(BinaryExpression node, bool isNotEqual)
    {
        var left = VisitComparisonOperand(node.Left);
        var right = VisitComparisonOperand(node.Right);

        // When both sides are constants, fold using value equality (not reference equality of boxed values).
        if (left is ConstantExpression { Value: var lv } && right is ConstantExpression { Value: var rv })
            return Expression.Constant(isNotEqual ? !Equals(lv, rv) : Equals(lv, rv));

        // A boolean predicate (comparison, IS NULL check, method call, ...) compared to a boolean constant:
        // fold to the predicate itself or to its negation. Raw boolean columns are compared as values below.
        if (TryFoldBoolPredicate(left, right, isNotEqual) is { } folded)
            return folded;
        if (TryFoldBoolPredicate(right, left, isNotEqual) is { } foldedSwapped)
            return foldedSwapped;

        return (left, right) switch
        {
            // When the left side is a constant and the right is not, put the constant on the right.
            (ConstantExpression l, var r) => MakeEquality(r, l, isNotEqual),

            // When the left side is not a constant, the right is a constant, and their types are different,
            // but the non-nullable type of left is the same as the type of right, convert right to left's type.
            (var l, ConstantExpression r) when l.Type != r.Type && Nullable.GetUnderlyingType(l.Type) == r.Type => MakeEquality(Expression.Convert(r, l.Type), l, isNotEqual),

            // When left is not a constant, right is a member expression, and their types are different,
            // but the non-nullable type of left is the same as the type of right, convert right to left's type.
            (var l, MemberExpression r) when l.Type != r.Type && Nullable.GetUnderlyingType(l.Type) == r.Type => MakeEquality(Expression.Convert(r, l.Type), l, isNotEqual),

            // Default case: compare both sides as they are.
            var (l, r) => MakeEquality(l, r, isNotEqual)
        };

        static Expression MakeEquality(Expression l, Expression r, bool notEqual) => notEqual ? Expression.NotEqual(l, r) : Expression.Equal(l, r);
    }

    /// <summary>
    /// Folds <c>predicate == bool constant</c> (or <c>!=</c>) to the predicate or its negation.
    /// </summary>
    /// <param name="predicate">The candidate predicate side.</param>
    /// <param name="constant">The candidate constant side.</param>
    /// <param name="isNotEqual"><c>true</c> for inequality; <c>false</c> for equality.</param>
    /// <returns>The folded expression or null if the pattern does not apply.</returns>
    private static Expression? TryFoldBoolPredicate(Expression predicate, Expression constant, bool isNotEqual)
    {
        if (constant is not ConstantExpression { Value: bool b } || predicate is ConstantExpression || predicate.Type != typeof(bool) || IsRawBoolValue(predicate))
            return null;

        return b != isNotEqual ? predicate : Negate(predicate);
    }

    /// <summary>
    /// Visits an operand of an equality/inequality comparison. Boolean columns are kept as raw column values
    /// (instead of being rewritten to <c>column = true</c>) so that they can be compared as values.
    /// </summary>
    /// <param name="node">The operand to visit.</param>
    /// <returns>The visited operand.</returns>
    private Expression VisitComparisonOperand(Expression node) => node switch
    {
        // x.BoolColumn
        MemberExpression { Expression: ParameterExpression } me when me.Type == typeof(bool) => me,

        // (bool?)x.BoolColumn
        UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression { Expression: ParameterExpression } me } when me.Type == typeof(bool) => node,

        // x.BoolColumnN.Value
        MemberExpression { Expression: MemberExpression { Expression: ParameterExpression } ime, Member: PropertyInfo { Name: "Value", DeclaringType: { } declaringType } } me
            when me.Type == typeof(bool) && IsNullableT(declaringType) => Expression.Convert(ime, typeof(bool)),

        _ => Visit(node)
    };

    /// <summary>
    /// Determines whether the expression is a raw boolean column value (as opposed to a boolean predicate).
    /// </summary>
    /// <param name="node">The expression to check.</param>
    /// <returns>True if the expression is a raw column value.</returns>
    private static bool IsRawBoolValue(Expression node) => node is
        MemberExpression { Expression: ParameterExpression } or
        UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression { Expression: ParameterExpression } };

    /// <summary>
    /// Visits a member access expression and attempts to reduce common patterns such as boolean shorthand and nullable Value/HasValue access.
    /// </summary>
    /// <param name="node">The member expression to visit.</param>
    /// <returns>The reduced expression or defers to base visitor for unsupported cases.</returns>
    protected override Expression VisitMember(MemberExpression node) => node switch
    {
        // If the member is a direct property or field of a parameter (e.g., x.MyProperty) and its type is bool,
        // create an expression to check if it's equal to true.
        { Expression: ParameterExpression } when node.Type == typeof(bool) => Expression.Equal(node, Expression.Constant(true)),

        // If the member is a nested property, and it represents the 'HasValue' property of a Nullable type,
        // create an expression to check if the parent member is null.
        { Expression: MemberExpression { Expression: ParameterExpression } ime } when IsNullableHasValue(node) => Expression.NotEqual(ime, Expression.Constant(null)),

        // Allow access to Nullable<T>.Value: convert parent nullable expression to underlying type.
        // If the member is the 'Value' property of a nullable (e.g. x.BoolN.Value) and its type is bool,
        // return an equality comparison against true. This makes nullable boolean value usage
        // behave like a boolean equality (column = true) during SQL generation.
        { Expression: MemberExpression { Expression: ParameterExpression } ime, Member: PropertyInfo { Name: "Value", DeclaringType: { } declaringType } } when IsNullableT(declaringType)
            => node.Type == typeof(bool) ? Expression.Equal(Expression.Convert(ime, node.Type), Expression.Constant(true)) : Expression.Convert(ime, node.Type),

        // If the member is a nested property (e.g., x.MyProperty.MySubProperty), throw an exception as it's not supported.
        { Expression: MemberExpression { Expression: ParameterExpression } } => throw new NotSupportedException("Nested properties not supported"),

        // If the member is a static field, return its value as a constant expression.
        { Member: FieldInfo { IsStatic: true } fi } => Expression.Constant(fi.GetValue(null)),

        // If the member is a static property, return its value as a constant expression.
        { Member: PropertyInfo { GetMethod.IsStatic: true } pi } => Expression.Constant(pi.GetValue(null)),

        // If the member represents the 'HasValue' property of a Nullable type and its parent expression is a constant,
        // return a constant expression indicating if the parent's value is not null.
        { Expression: { } ie } when IsNullableHasValue(node) && Visit(ie) is ConstantExpression { Value: var c } => Expression.Constant(c is not null),

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
        return input is { Member: { Name: "HasValue", DeclaringType: { } declaringType } } && IsNullableT(declaringType);
    }

    /// <summary>
    /// Checks whether the supplied type is a non-nullable value type.
    /// </summary>
    /// <param name="t">Type to check.</param>
    /// <returns>True if <paramref name="t"/> is a non-nullable value type; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNonNullableValueType(Type t) => t.IsValueType && Nullable.GetUnderlyingType(t) is null;

    /// <summary>
    /// Checks whether the supplied type is a nullable generic (Nullable&lt;T&gt;).
    /// </summary>
    /// <param name="t">Type to check.</param>
    /// <returns>True if <paramref name="t"/> is Nullable&lt;T&gt;; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsNullableT(Type t) => Nullable.GetUnderlyingType(t) is not null;
}
