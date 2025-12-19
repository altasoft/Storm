using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Interfaces;

#if NET8_0_OR_GREATER

using AltaSoft.DomainPrimitives;

#endif

namespace AltaSoft.Storm.Helpers;

/// <summary>
/// The SqlWhereStatementGenerator class is responsible for generating SQL WHERE statements based on LINQ expressions.
/// It extends the ExpressionVisitor class and overrides its methods to handle different types of expressions.
/// The class maintains a StringBuilder to build the SQL statement and a parameter index to generate parameter names.
/// </summary>
internal sealed class SqlStatementGenerator : ExpressionVisitor
{
    private const string ParamPrefix = "@p";
    private int _paramIndex;

    private readonly StringBuilder _builder;
    private readonly StormColumnDef[] _columns;
    private readonly string? _tableAlias;
    private readonly IVirtualStormDbCommand _command;
    private string? _currentMemberName;

    /// <summary>
    /// Initializes a new instance of the SqlWhereStatementGenerator class.
    /// </summary>
    private SqlStatementGenerator(StringBuilder builder, IVirtualStormDbCommand command, StormColumnDef[] columns, string? tableAlias)
    {
        _builder = builder;
        _command = command;
        _columns = columns;
        _tableAlias = tableAlias is null ? null : tableAlias + '.';
    }

    /// <summary>
    /// Converts an expression into a SQL value statement for a given database command and column definitions.
    /// </summary>
    /// <typeparam name="T">The type of the data to bind.</typeparam>
    /// <param name="command">The database command.</param>
    /// <param name="expression">The expression to convert.</param>
    /// <param name="columns">The column definitions.</param>
    /// <param name="tableAlias">An optional alias for the table.</param>
    /// <param name="paramIndex">The starting index for parameterization in the SQL command.</param>
    /// <param name="sb">The StringBuilder to append the elements to.</param>
    /// <returns>The SQL value statement.</returns>

    public static void GenerateValueSql<T>(IVirtualStormDbCommand command, Expression<Func<T, object?>> expression, StormColumnDef[] columns, string? tableAlias, ref int paramIndex, StringBuilder sb)
    {
        new SqlStatementGenerator(sb, command, columns, tableAlias).GenerateSql(expression.Body, ref paramIndex);
    }

    /// <summary>
    /// Converts an expression into a SQL WHERE statement for a given database command and column definitions.
    /// </summary>
    /// <typeparam name="T">The type of the data to bind.</typeparam>
    /// <param name="command">The database command.</param>
    /// <param name="expressions">The expressions to convert.</param>
    /// <param name="columns">The column definitions.</param>
    /// <param name="tableAlias">An optional alias for the table.</param>
    /// <param name="paramIndex">The starting index for parameterization in the SQL command.</param>
    /// <param name="sb">The StringBuilder to append the elements to.</param>
    /// <returns>The SQL WHERE statement.</returns>
    public static void GenerateWhereSql<T>(IVirtualStormDbCommand command, List<Expression<Func<T, bool>>> expressions, StormColumnDef[] columns, string? tableAlias, ref int paramIndex, StringBuilder sb) where T : IDataBindable
    {
        if (expressions.Count == 0)
        {
            sb.Append("1=1");
            return;
        }

        for (var i = 0; i < expressions.Count; i++)
        {
            if (i > 0)
                sb.Append(" AND ");
            var expression = expressions[i];

            switch (PredicateExpressionReducer.Instance.Visit(expression))
            {
                case Expression<Func<T, bool>> { Body: ConstantExpression { Value: bool c } }:
                    sb.Append(c ? "1=1" : "1=0");
                    break;

                case { } e:
                    new SqlStatementGenerator(sb, command, columns, tableAlias).GenerateSql(e, ref paramIndex);
                    break;

                default:
                    throw new InvalidOperationException("Invalid expression");
            }
        }
    }

    /// <summary>
    /// Converts the given expression to a SQL string representation.
    /// </summary>
    /// <param name="expression">The expression to convert.</param>
    /// <param name="paramIndex">The starting index for parameterization in the SQL command.</param>
    /// <returns>The SQL string representation of the expression.</returns>
    private void GenerateSql(Expression expression, ref int paramIndex)
    {
        _paramIndex = paramIndex;
        Visit(expression);
        paramIndex = _paramIndex;
    }

    /// <inheritdoc/>
    protected override Expression VisitUnary(UnaryExpression node)
    {
        switch (node)
        {
            case { NodeType: ExpressionType.Convert, Operand: var operand }: return Visit(operand) ?? throw new NotSupportedException("Invalid expression");
            case { NodeType: ExpressionType.Not, Operand: var operand }:
                _builder.Append("NOT (");
                Visit(operand);
                _builder.Append(')');
                return node;

            case { NodeType: ExpressionType.Negate, Operand: var operand }:
                _builder.Append("-(");
                Visit(operand);
                _builder.Append(')');
                return node;

            default: throw new NotSupportedException($"The unary operator '{node.NodeType}' is not supported");
        }
    }

    /// <inheritdoc/>
    protected override Expression VisitBinary(BinaryExpression node)
    {
        _builder.Append('(');

        Visit(node.Left);

        _builder.Append(
            node.NodeType switch
            {
                ExpressionType.And => " & ",
                ExpressionType.AndAlso => " AND ",
                ExpressionType.Or => " | ",
                ExpressionType.OrElse => " OR ",
                ExpressionType.Equal => node.Right is ConstantExpression(null) ? " IS " : " = ",
                ExpressionType.NotEqual => node.Right is ConstantExpression(null) ? " IS NOT " : " <> ",
                ExpressionType.LessThan => " < ",
                ExpressionType.LessThanOrEqual => " <= ",
                ExpressionType.GreaterThan => " > ",
                ExpressionType.GreaterThanOrEqual => " >= ",
                ExpressionType.ExclusiveOr => " ^ ",
                ExpressionType.Add => " + ",
                ExpressionType.Subtract => " - ",
                ExpressionType.Multiply => " * ",
                ExpressionType.Divide => " / ",
                ExpressionType.Modulo => " % ",
                //ExpressionType.Coalesce => expr,
                //ExpressionType.Conditional => expr,
                //ExpressionType.Constant => expr,
                //ExpressionType.LeftShift => expr,
                _ => throw new NotSupportedException($"The binary operator '{node.NodeType}' is not supported")
            });

        Visit(node.Right);
        _builder.Append(')');

        return node;
    }
    /// <inheritdoc/>
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        switch (node.Object)
        {
            // Support instance string methods on member access, including when parameter is cast: ((T)$x).Prop.Method(...)
            case MemberExpression { Expression: var expr } m when IsMemberAccessOnParameter(expr):
                {
                    var memberName = m.Member.Name;

                    if (node.Method.Name is "Contains" or "StartsWith" or "EndsWith" && node.Arguments.Count > 0)
                    {
                        switch (GetExpressionValue(node.Arguments[0]))
                        {
                            case null:
                                _builder.Append(GetColumnName(memberName)).Append(" IS NULL");
                                return node;

                            case { } arg:
                                _builder.Append(GetColumnName(memberName)).Append(" LIKE ");
                                _currentMemberName = memberName;

                                switch (node.Method.Name)
                                {
                                    case "Contains":
                                        _builder.Append("'%'+");
                                        AddConstantParam(arg.ToString(), typeof(string));
                                        _builder.Append("+'%'");
                                        break;

                                    case "StartsWith":
                                        AddConstantParam(arg.ToString(), typeof(string));
                                        _builder.Append("+'%'");
                                        break;

                                    case "EndsWith":
                                        _builder.Append("'%'+");
                                        AddConstantParam(arg.ToString(), typeof(string));
                                        break;
                                }

                                return node;
                        }
                    }
                    break;
                }

            // Support static 'In' helper: SqlWhereExt.In(x => x.Prop, collection)
            case null when string.Equals(node.Method.Name, nameof(SqlWhereExt.In), StringComparison.Ordinal) && node.Arguments.Count >= 2:
                {
                    if (node.Arguments[0] is not MemberExpression arg)
                        return node;

                    switch (GetExpressionValue(node.Arguments[1]))
                    {
                        case IEnumerable list:
                            var values = list.Cast<object?>().ToList();
                            if (values.Count == 0)
                            {
                                _builder.Append("1=0");
                                return node;
                            }

                            _builder.Append(GetColumnName(arg.Member.Name)).Append(" IN (");

                            _currentMemberName = arg.Member.Name;

                            AddConstantParam(values[0], arg.Type);

                            for (var i = 1; i < values.Count; i++)
                            {
                                _builder.Append(',');
                                AddConstantParam(values[i], arg.Type);
                            }

                            _builder.Append(')');
                            return node;

                        default:
                            _builder.Append(GetColumnName(arg.Member.Name)).Append(" IS NULL");
                            return node;
                    }
                }
        }

        throw new NotSupportedException($"The method '{node.Method.Name}' is not supported");
    }

    /// <inheritdoc/>
    protected override Expression VisitConstant(ConstantExpression node)
    {
        AddConstantParam(node.Value, node.Type);
        return node;
    }

    /// <inheritdoc/>
    protected override Expression VisitMember(MemberExpression node)
    {
        // Support direct parameter access and parameter wrapped in a Convert (cast) expression
        if (IsMemberAccessOnParameter(node.Expression))
        {
            var name = node.Member.Name;
            _builder.Append(GetColumnName(name));
            _currentMemberName = node.Member.Name;
        }
        else
        {
            // This is a captured variable or static field/property
            var value = GetExpressionValue(node);
            AddConstantParam(value, node.Type);
        }
        return node;
    }

    /// <summary>
    /// Retrieves the value from the given expression.
    /// </summary>
    /// <param name="expression">The expression to retrieve the value from.</param>
    /// <returns>The value obtained from the expression.</returns>
    private static object? GetExpressionValue(in Expression expression)
    {
        // Handle array/new-array expressions (e.g. new[] {1,2,3})
        if (expression is NewArrayExpression nae)
        {
            var elemType = expression.Type.GetElementType() ?? typeof(object);
            var arr = Array.CreateInstance(elemType, nae.Expressions.Count);
            for (var i = 0; i < nae.Expressions.Count; i++)
            {
                var v = GetExpressionValue(nae.Expressions[i]);
                // Convert element to array element type if necessary
                if (v is not null && elemType != v.GetType())
                    v = Convert.ChangeType(v, elemType, CultureInfo.InvariantCulture);
                arr.SetValue(v, i);
            }
            return arr;
        }

        static Func<object?, object?> Compose(Func<object?, object?> f, Func<object?, object?> g) => x => x is not null ? g(f(x)) : null;

        Func<object?, object?> get = x => x;

        var expr = expression;

        while (true)
        {
            switch (expr)
            {
                case ConstantExpression c: return get(c.Value);
                case MemberExpression { Member: FieldInfo { IsStatic: true } fi }: return get(fi.GetValue(null));
                case MemberExpression { Member: PropertyInfo { GetMethod.IsStatic: true } pi }: return get(pi.GetValue(null));
                case MemberExpression { Expression: var e, Member: FieldInfo fi }:
                    get = Compose(fi.GetValue, get);
                    expr = e;
                    break;

                case MemberExpression { Expression: var e, Member: PropertyInfo pi }:
                    get = Compose(pi.GetValue, get);
                    expr = e;
                    break;

                default: throw new StormException($"Cannot get value from expression: '{expr}'");
            }
        }
    }

    /// <summary>
    /// Retrieves the column name associated with the given property name.
    /// Throws a StormException if the property name is not found in the columns.
    /// </summary>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The column name associated with the property name.</returns>
    private string GetColumnName(string propertyName)
    {
        var columnName = Array.Find(_columns, x => string.Equals(x.PropertyName, propertyName, StringComparison.Ordinal))?.ColumnName
                         ?? throw new StormException($"Cannot find {propertyName} property in columns");
        return _tableAlias is null ? columnName : _tableAlias + columnName;
    }

    /// <summary>
    /// Adds a constant parameter to the SQL query.
    /// </summary>
    /// <param name="value">The value of the constant parameter.</param>
    /// <param name="type">The type of the constant parameter. If not provided, the type will be inferred from the value.</param>
    private void AddConstantParam(object? value, Type? type = null)
    {
        if (value is null)
        {
            _builder.Append("NULL");
            return;
        }

        type ??= value.GetType();

        // Treat Nullable<T> as T for parameter generation
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType is not null)
        {
            type = underlyingType;
        }

        if (type is { IsPrimitive: false, IsValueType: false } && !type.IsDomainPrimitive() && type != typeof(string))
            throw new NotSupportedException($"The constant for '{value}' is not supported");

        var column = Array.Find(_columns, x => string.Equals(x.PropertyName, _currentMemberName, StringComparison.Ordinal));
        var paramName = ParamPrefix + _paramIndex++.ToString(CultureInfo.InvariantCulture);

        if (column is not null)
        {
            var dbType = ToVariableLengthDbType(column.DbType);
            
            var size = GetParamSize(dbType, column.Size, value);
            _command.AddDbParameter(paramName, dbType, size, value);
        }
        else
        {
            if (type.IsEnum)
            {
                var enumType = Enum.GetUnderlyingType(type);
                value = Convert.ChangeType(value, enumType, CultureInfo.InvariantCulture);
                type = enumType;
            }

#if NET8_0_OR_GREATER
            if (value is IDomainValue d)
            {
                type = d.GetUnderlyingPrimitiveType();
                value = d.GetUnderlyingPrimitiveValue();
            }
#endif

            var dbType = type.ToUnifiedDbType();
            dbType = ToVariableLengthDbType(dbType);

            var size = GetParamSize(dbType, null, value);
            _command.AddDbParameter(paramName, dbType, size, value);
        }
        _builder.Append(paramName);
        return;

        static int GetParamSize(UnifiedDbType dbType, int? columnSize, object? value)
        {
            if (!dbType.HasMaxSize())
                return 0;

            var valueSize = value switch
            {
                string str => str.Length,
                byte[] arr => arr.Length,
                SqlRowVersion _ => 8,
                SqlLogSequenceNumber _ => 10,
                _ => 0
            };

            if (!columnSize.HasValue)
                return valueSize;

            return valueSize > columnSize.Value ? valueSize : columnSize.Value;
        }
    }

    private static UnifiedDbType ToVariableLengthDbType(UnifiedDbType dbType)
    {
        return dbType switch
        {
            UnifiedDbType.StringFixedLength => UnifiedDbType.String,
            UnifiedDbType.AnsiStringFixedLength => UnifiedDbType.AnsiString,
            UnifiedDbType.Binary => UnifiedDbType.VarBinary,

            _ => dbType
        };
    }

    private static bool IsMemberAccessOnParameter(Expression? expr)
    {
        return expr is ParameterExpression or
            UnaryExpression { NodeType: ExpressionType.Convert, Operand: ParameterExpression };
    }
}
