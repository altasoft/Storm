using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using AltaSoft.Storm.Attributes;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace AltaSoft.Storm.Helpers;

/// <summary>
/// Generates SQL statements based on an OData filter expression.
/// </summary>
public sealed class ODataFilterStatementGenerator : QueryNodeVisitor<QueryNode>
{
    private const string ParamPrefix = "@p";
    private int _paramIndex;

    private readonly StringBuilder _builder;
    private readonly Dictionary<string, StormColumnDef> _propertyLookup;
    private readonly IVirtualStormDbCommand _command;
    private StormColumnDef? _lastIdentifierVisitedColumnDef;
    private readonly string? _tableAlias;

    private ODataFilterStatementGenerator(StringBuilder builder, IVirtualStormDbCommand command,
        Dictionary<string, StormColumnDef> propertyLookup, int paramIndex, string? tableAlias)
    {
        _builder = builder;
        _command = command;
        _propertyLookup = propertyLookup;
        _paramIndex = paramIndex;
        _tableAlias = tableAlias is null ? null : tableAlias + '.';
    }

    /// <summary>
    /// Generates SQL query based on the provided IVirtualStormDbCommand, filter, column definitions, table alias, parameter index, and StringBuilder.
    /// </summary>
    internal static void GenerateSql(IVirtualStormDbCommand command, string filter, StormColumnDef[] columnDefs, string? tableAlias, ref int paramIndex, StringBuilder sb)
    {
        var model = new EdmModel();

        var entity = model.AddEntityType("", "EntityName");
        var propertyLookup = new Dictionary<string, StormColumnDef>();

        var enums = new HashSet<Type>();

        foreach (var column in columnDefs.Where(x => x.SaveAs == SaveAs.Default))
        {
            var propertyName = column.PropertyName;
            var subPropertyName = column.SubPropertyName;

            if (subPropertyName is not null)
            {
                propertyName = propertyName + "___" + subPropertyName;
                filter = filter.Replace(propertyName + '.' + subPropertyName, propertyName);
            }

            if (column.PropertyType.IsEnum)
            {
                var enumType = column.PropertyType;
                if (!enums.Contains(enumType))
                {
                    var isFlagged = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);

                    var type = new EdmEnumType("", enumType.Name, isFlags: isFlagged);
                    foreach (var e in Enum.GetValues(enumType))
                        type.AddMember(e.ToString(), new EdmEnumMemberValue(Convert.ToInt64(e, CultureInfo.InvariantCulture)));

                    model.AddElement(type);
                    enums.Add(enumType);

                    entity.AddStructuralProperty(propertyName, new EdmEnumTypeReference(type, isNullable: column.IsNullable));
                }
            }
            else
            {
                entity.AddStructuralProperty(propertyName, ColumnDefToPrimitiveTypeKind(column));
            }
            propertyLookup.Add(propertyName, column);
        }

        var parser = new ODataQueryOptionParser(model, entity, null, new Dictionary<string, string> { ["$filter"] = filter })
        {
            Resolver = new ODataUriResolver { EnableCaseInsensitive = true }
        };

        var filterClause = parser.ParseFilter();

        var translator = new ODataFilterStatementGenerator(sb, command, propertyLookup, paramIndex, tableAlias);

        filterClause.Expression.Accept(translator);

        paramIndex = translator._paramIndex;
        return;

        static EdmPrimitiveTypeKind ColumnDefToPrimitiveTypeKind(StormColumnDef column)
        {
            return column switch
            {
                { PropertyType.IsEnum: true } => EdmPrimitiveTypeKind.String,
                { DbType: UnifiedDbType.Boolean } => EdmPrimitiveTypeKind.Boolean,
                { DbType: UnifiedDbType.UInt8 } => EdmPrimitiveTypeKind.Byte,
                { DbType: UnifiedDbType.Int8 } => EdmPrimitiveTypeKind.SByte,
                { DbType: UnifiedDbType.UInt16 } => EdmPrimitiveTypeKind.Int16,
                { DbType: UnifiedDbType.Int16 } => EdmPrimitiveTypeKind.Int16,
                { DbType: UnifiedDbType.UInt32 } => EdmPrimitiveTypeKind.Int32,
                { DbType: UnifiedDbType.Int32 } => EdmPrimitiveTypeKind.Int32,
                { DbType: UnifiedDbType.UInt64 } => EdmPrimitiveTypeKind.Int64,
                { DbType: UnifiedDbType.Int64 } => EdmPrimitiveTypeKind.Int64,

                { DbType: UnifiedDbType.AnsiString } => EdmPrimitiveTypeKind.String,
                { DbType: UnifiedDbType.String } => EdmPrimitiveTypeKind.String,
                { DbType: UnifiedDbType.AnsiStringFixedLength } => EdmPrimitiveTypeKind.String,
                { DbType: UnifiedDbType.StringFixedLength } => EdmPrimitiveTypeKind.String,
                //{ DbType: UnifiedDbType.CompressedString } => EdmPrimitiveTypeKind.String,

                { DbType: UnifiedDbType.Currency } => EdmPrimitiveTypeKind.Decimal,
                { DbType: UnifiedDbType.Single } => EdmPrimitiveTypeKind.Decimal,
                { DbType: UnifiedDbType.Double } => EdmPrimitiveTypeKind.Decimal,
                { DbType: UnifiedDbType.Decimal } => EdmPrimitiveTypeKind.Decimal,

                { DbType: UnifiedDbType.SmallDateTime } => EdmPrimitiveTypeKind.DateTimeOffset,
                { DbType: UnifiedDbType.DateTime } => EdmPrimitiveTypeKind.DateTimeOffset,
                { DbType: UnifiedDbType.DateTime2 } => EdmPrimitiveTypeKind.DateTimeOffset,
                { DbType: UnifiedDbType.DateTimeOffset } => EdmPrimitiveTypeKind.DateTimeOffset,
                { DbType: UnifiedDbType.Date } => EdmPrimitiveTypeKind.Date,
                { DbType: UnifiedDbType.Time } => EdmPrimitiveTypeKind.TimeOfDay,

                { DbType: UnifiedDbType.Guid } => EdmPrimitiveTypeKind.Guid,

                { DbType: UnifiedDbType.AnsiXml } => EdmPrimitiveTypeKind.String,
                { DbType: UnifiedDbType.Xml } => EdmPrimitiveTypeKind.String,
                { DbType: UnifiedDbType.AnsiJson } => EdmPrimitiveTypeKind.String,
                { DbType: UnifiedDbType.Json } => EdmPrimitiveTypeKind.String,
                { DbType: UnifiedDbType.AnsiText } => EdmPrimitiveTypeKind.String,
                { DbType: UnifiedDbType.Text } => EdmPrimitiveTypeKind.String,

                { DbType: UnifiedDbType.VarBinary } => EdmPrimitiveTypeKind.Binary,
                { DbType: UnifiedDbType.Binary } => EdmPrimitiveTypeKind.Binary,
                { DbType: UnifiedDbType.Blob } => EdmPrimitiveTypeKind.Binary,

                { DbType: var other } => throw new ArgumentOutOfRangeException(nameof(column), other, null),
                _ => throw new ArgumentNullException(nameof(column))
            };
        }
    }

    /// <inheritdoc/>
    public override QueryNode Visit(ConstantNode nodeIn)
    {
        if (_lastIdentifierVisitedColumnDef is null)
            throw new InvalidOperationException("Unable to infer constant type");

        if (nodeIn.Value is null)
        {
            _builder.Append("NULL");
            return nodeIn;
        }

        var dbType = _lastIdentifierVisitedColumnDef.DbType;
        var value = nodeIn.Value;

        if (_lastIdentifierVisitedColumnDef.PropertyType.IsEnum)
        {
            if (!Enum.TryParse(_lastIdentifierVisitedColumnDef.PropertyType, ((ODataEnumValue)value).Value, true, out var enumValue))
                throw new ODataException($"Invalid '{_lastIdentifierVisitedColumnDef.PropertyType.Name}' enum value: '{nodeIn.Value}'");

            value = dbType switch
            {
                UnifiedDbType.UInt8 => Convert.ChangeType(enumValue, TypeCode.Byte, CultureInfo.InvariantCulture),
                UnifiedDbType.Int8 => Convert.ChangeType(enumValue, TypeCode.SByte, CultureInfo.InvariantCulture),
                UnifiedDbType.UInt16 => Convert.ChangeType(enumValue, TypeCode.UInt16, CultureInfo.InvariantCulture),
                UnifiedDbType.Int16 => Convert.ChangeType(enumValue, TypeCode.Int16, CultureInfo.InvariantCulture),
                UnifiedDbType.UInt32 => Convert.ChangeType(enumValue, TypeCode.UInt32, CultureInfo.InvariantCulture),
                UnifiedDbType.Int32 => Convert.ChangeType(enumValue, TypeCode.Int32, CultureInfo.InvariantCulture),
                UnifiedDbType.UInt64 => Convert.ChangeType(enumValue, TypeCode.UInt64, CultureInfo.InvariantCulture),
                UnifiedDbType.Int64 => Convert.ChangeType(enumValue, TypeCode.Int64, CultureInfo.InvariantCulture),
                _ => throw new NotSupportedException($"Not supported enum underlying type: {_lastIdentifierVisitedColumnDef.DbType}")
            };
        }
        else
        {
            value = value switch
            {
                //Microsoft.OData.Edm.Date
                Date { Year: var year, Month: var month, Day: var day } => new DateOnly(year, month, day),
                //Microsoft.OData.Edm.TimeOfDay
                TimeOfDay { Ticks: var ticks } => new TimeOnly(ticks),
                DateTimeOffset { DateTime: var dt } when dbType is UnifiedDbType.DateTime or UnifiedDbType.DateTime2 or UnifiedDbType.SmallDateTime => dt,
                _ => value
            };
        }

        var paramName = ParamPrefix + _paramIndex++.ToString(CultureInfo.InvariantCulture);

        AddDbParameter(paramName, dbType, _lastIdentifierVisitedColumnDef.Size, value);
        _builder.Append(paramName);

        return nodeIn;
    }

    /// <inheritdoc/>
    public override QueryNode Visit(BinaryOperatorNode nodeIn)
    {
        return nodeIn switch
        {
            { Left: ConvertNode { Source: var s } } => new BinaryOperatorNode(nodeIn.OperatorKind, s, nodeIn.Right).Accept(this),
            { Right: ConvertNode { Source: var s } } => new BinaryOperatorNode(nodeIn.OperatorKind, nodeIn.Left, s).Accept(this),
            { Left: ConstantNode _, Right: ConstantNode _ } => throw new ArgumentException($"Both parts of binary operation '{nodeIn.OperatorKind}' are constants"),
            { Left: ConstantNode l, Right: SingleValuePropertyAccessNode r } => new BinaryOperatorNode(ReverseComparison(nodeIn.OperatorKind), r, l).Accept(this),
            _ => VisitBinary(nodeIn)
        };

        QueryNode VisitBinary(BinaryOperatorNode input)
        {
            VisitInnerNode(input.Left);

            if (input.Right.IsNullConstant() && input.OperatorKind is BinaryOperatorKind.Equal or BinaryOperatorKind.NotEqual)
            {
                _builder.Append(' ').Append(ToNullComparison(nodeIn.OperatorKind));

                return nodeIn;
            }

            _builder.Append(' ').Append(ToSqlBinaryOpPre(nodeIn.OperatorKind)).Append(' ');

            VisitInnerNode(nodeIn.Right);

            _builder.Append(ToSqlBinaryOpPost(nodeIn.OperatorKind));

            return nodeIn;

            void VisitInnerNode(QueryNode innerNode)
            {
                if (innerNode is BinaryOperatorNode { OperatorKind: var operatorKind } && input.OperatorKind.HasHigherPriorityThan(operatorKind))
                {
                    _builder.Append('(');
                    innerNode.Accept(this);
                    _builder.Append(')');
                }
                else
                {
                    innerNode.Accept(this);
                }
            }
        }

        static BinaryOperatorKind ReverseComparison(BinaryOperatorKind value) => value switch
        {
            BinaryOperatorKind.GreaterThan => BinaryOperatorKind.LessThanOrEqual,
            BinaryOperatorKind.GreaterThanOrEqual => BinaryOperatorKind.LessThan,
            BinaryOperatorKind.LessThan => BinaryOperatorKind.GreaterThanOrEqual,
            BinaryOperatorKind.LessThanOrEqual => BinaryOperatorKind.GreaterThan,
            _ => value
        };

        static string ToNullComparison(BinaryOperatorKind input) => input switch
        {
            BinaryOperatorKind.Equal => "IS NULL",
            BinaryOperatorKind.NotEqual => "IS NOT NULL",
            _ => throw new ArgumentException($"Invalid operator {input} usage for 'NULL' constant")
        };

        static string ToSqlBinaryOpPre(BinaryOperatorKind input) => input switch
        {
            BinaryOperatorKind.And => "AND",
            BinaryOperatorKind.Or => "OR",
            BinaryOperatorKind.Equal => "=",
            BinaryOperatorKind.NotEqual => "<>",
            BinaryOperatorKind.GreaterThan => ">",
            BinaryOperatorKind.GreaterThanOrEqual => ">=",
            BinaryOperatorKind.LessThan => "<",
            BinaryOperatorKind.LessThanOrEqual => "<=",
            BinaryOperatorKind.Has => "&",
            _ => throw new NotSupportedException($"Operator: {input} not supported")
        };

        static string ToSqlBinaryOpPost(BinaryOperatorKind input) => input switch
        {
            BinaryOperatorKind.Has => " != 0",
            _ => ""
        };
    }

    /// <inheritdoc/>
    public override QueryNode Visit(SingleValuePropertyAccessNode nodeIn)
    {
        if (!_propertyLookup.TryGetValue(nodeIn.Property.Name, out var metadata))
            throw new ArgumentException($"Unknown identifier: '{nodeIn.Property.Name}'", nameof(nodeIn));

        _lastIdentifierVisitedColumnDef = metadata;
        if (_tableAlias is not null)
            _builder.Append(_tableAlias);
        _builder.Append(metadata.ColumnName);
        return nodeIn;
    }

    /// <inheritdoc/>
    public override QueryNode Visit(UnaryOperatorNode nodeIn)
    {
        return nodeIn switch
        {
            { Operand: ConvertNode { Source: var src } } => new UnaryOperatorNode(nodeIn.OperatorKind, src).Accept(this),
            { Operand: UnaryOperatorNode unary } when nodeIn.OperatorKind == unary.OperatorKind => unary.Operand.Accept(this),
            { OperatorKind: UnaryOperatorKind.Negate, Operand: var o } => Negate("-", o, this),
            { Operand: BinaryOperatorNode { OperatorKind: BinaryOperatorKind.Equal } o } => new BinaryOperatorNode(BinaryOperatorKind.NotEqual, o.Left, o.Right).Accept(this),
            { Operand: BinaryOperatorNode { OperatorKind: BinaryOperatorKind.NotEqual } o } => new BinaryOperatorNode(BinaryOperatorKind.Equal, o.Left, o.Right).Accept(this),
            { Operand: var o } => Negate("NOT", o, this),
            _ => throw new NotSupportedException($"Unary operator: {nodeIn.OperatorKind} not supported")
        };

        static QueryNode Negate(string negOp, QueryNode operand, ODataFilterStatementGenerator visitor)
        {
            visitor._builder.Append(negOp)
                .Append('(');

            var nodeOut = operand.Accept(visitor);

            visitor._builder.Append(')');

            return nodeOut;
        }
    }

    /// <inheritdoc/>
    public override QueryNode Visit(SingleValueFunctionCallNode nodeIn)
    {
        var parameters = nodeIn.Parameters.ToArray();

        parameters[0].Accept(this);

        switch (parameters[1])
        {
            case ConstantNode { Value: string constant }:
                _builder.Append(" LIKE ");
                parameters[1] = new ConstantNode(string.Format(CultureInfo.InvariantCulture, ToLikePattern(nodeIn.Name), constant)).Accept(this);
                return new SingleValueFunctionCallNode(nodeIn.Name, nodeIn.Functions, parameters, nodeIn.TypeReference, nodeIn.Source);

            default: throw new NotSupportedException($"function '{nodeIn.Name}' argument 2 is not string constant");
        }

        static string ToLikePattern(string input) => input switch
        {
            "startswith" => "{0}%",
            "endswith" => "%{0}",
            "contains" => "%{0}%",
            _ => throw new NotSupportedException($"Function: '{input}' not supported")
        };
    }

    /// <summary>
    /// Adds a database parameter to the command.
    /// </summary>
    /// <param name="paramName">The name of the parameter.</param>
    /// <param name="dbType">The database type of the parameter.</param>
    /// <param name="size">The size of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddDbParameter(string paramName, UnifiedDbType dbType, int size, object? value) => _ = _command.AddDbParameter(paramName, dbType, size, value);
}

internal static class ODataExt
{
    internal static bool IsNullConstant(this QueryNode self)
    {
        var n = self;

        while (true)
        {
            switch (n)
            {
                case ConstantNode { Value: null }: return true;
                case ConvertNode { Source: var source }:
                    n = source;
                    break;

                default: return false;
            }
        }
    }

    internal static bool HasHigherPriorityThan(this BinaryOperatorKind self, BinaryOperatorKind another) =>
        (self, another) is (BinaryOperatorKind.And, BinaryOperatorKind.Or);
}
