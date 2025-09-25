using System;
using System.Collections.Generic;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

internal sealed class SelectFromCustomSqlStatement<T, TOrderBy, TPartialLoadFlags> : SelectFrom<T, TOrderBy, TPartialLoadFlags>
    where T : IDataBindable
    where TOrderBy : struct, Enum
    where TPartialLoadFlags : struct, Enum
{
    private readonly string _customSqlStatement;

    internal SelectFromCustomSqlStatement(StormContext context, int variant, string customSqlStatement, List<StormCallParameter>? callParameters) : base(context, variant)
    {
        _customSqlStatement = customSqlStatement;
        CallParameters = callParameters;
    }

    internal SelectFromCustomSqlStatement(StormContext context, int variant, object[] keyValues, int keyId, string customSqlStatement, List<StormCallParameter>? callParameters) : base(context, variant, keyValues, keyId)
    {
        _customSqlStatement = customSqlStatement;
        CallParameters = callParameters;
    }

    protected override StormControllerBase GetController()
    {
        var ctrl = base.GetController();
        ctrl.QuotedObjectFullName = '(' + _customSqlStatement + ')';
        return ctrl;
    }
}
