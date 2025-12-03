using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using AltaSoft.Storm.Interfaces;

// ReSharper disable ConvertToPrimaryConstructor

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Represents parameters specific to update, delete and insert operations in ORM (Object-Relational Mapping).
/// </summary>
internal abstract class ModifyQueryParameters<T> : QueryParameters, IKeyAndWhereExpression<T> where T : IDataBindable
{
    protected ModifyQueryParameters(StormContext context, int variant) : base(context)
    {
        Variant = variant;
    }

    protected ModifyQueryParameters(StormContext context, int variant, string customQuotedObjectFullName) : base(context)
    {
        Variant = variant;
        _customQuotedObjectFullName = customQuotedObjectFullName;
    }

    private readonly string? _customQuotedObjectFullName;

    /// <summary>
    /// Variant of the controller
    /// </summary>
    protected internal int Variant;

    /// <summary>
    /// Array of objects representing key values, can be null.
    /// </summary>
    public object[]? KeyValues { get; set; }

    /// <summary>
    /// ID of the unique index/primary key in KeyColumnDefs array.
    /// </summary>
    public int? KeyId { get; protected set; }

    /// <summary>
    /// Gets or sets the expressions used for filtering entities of type T.
    /// </summary>
    public List<Expression<Func<T, bool>>>? WhereExpressions { get; set; }

    /// <summary>
    /// Gets the OData filter string used for filtering entities of type T.
    /// </summary>
    public string? ODataFilter { get; set; }

    /// <summary>
    /// Gets or sets the number of rows to be returned in the query.
    /// A null value indicates that all rows should be returned.
    /// This property is typically used for pagination.
    /// </summary>
    protected internal int? TopRows;

    /// <summary>
    /// List of parameters when executing table valued function or stored procedure.
    /// </summary>
    protected internal List<StormCallParameter>? CallParameters;

    /// <summary>
    /// Row to insert/update/delete
    /// </summary>
    protected T? RowValue;

    /// <summary>
    /// Rows to insert/update/delete
    /// </summary>
    protected IEnumerable<T>? RowValues;

    /// <summary>
    /// Retrieves the controller of type T from the StormControllerCache.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual StormControllerBase GetController()
    {
        var ctrl = StormControllerCache.Get<T>(Variant);

        if (_customQuotedObjectFullName is not null)
            ctrl.QuotedObjectFullName = _customQuotedObjectFullName;

        return ctrl;
    }
}
