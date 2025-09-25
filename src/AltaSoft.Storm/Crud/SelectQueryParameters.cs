using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using AltaSoft.Storm.Interfaces;

// ReSharper disable ConvertToPrimaryConstructor

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Represents parameters specific to select query operations in ORM (Object-Relational Mapping).
/// </summary>
internal abstract class SelectQueryParameters<T> : QueryParameters, IKeyAndWhereExpression<T> where T : IDataBindable
{
    protected SelectQueryParameters(StormContext context, int variant) : base(context)
    {
        Variant = variant;
    }

    protected SelectQueryParameters(StormContext context, int variant, List<StormCallParameter> callParameters) : this(context, variant)
    {
        CallParameters = callParameters;
        LoadDetailTables = false;
    }

    /// <summary>
    /// Variant of the controller
    /// </summary>
    protected int Variant;

    /// <summary>
    /// Array of objects representing key values, can be null.
    /// </summary>
    public object[]? KeyValues { get; protected set; }

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
    /// Gets or sets the number of rows to skip before starting to return rows in the query.
    /// A null value indicates no rows are skipped.
    /// This property is commonly used in conjunction with <see cref="TopRows"/> for implementing pagination.
    /// </summary>
    protected internal int? SkipRows;

    /// <summary>
    /// Gets or sets a boolean value indicating whether change tracking should automatically start.
    /// </summary>
    protected internal bool AutoStartChangeTracking;

    /// <summary>
    /// Represents a protected field named PartialLoadFlags of type uint with an initial value of uint.MaxValue.
    /// </summary>
    protected internal uint PartialLoadFlags = uint.MaxValue;

    /// <summary>
    /// Array of integers representing the column IDs used for ordering.
    /// Can be null if no specific order is defined.
    /// </summary>
    protected internal int[]? OrderByColumnIds;

    /// <summary>
    /// Gets or sets a boolean value indicating whether to load detail tables.
    /// Default value is true.
    /// </summary>
    protected internal bool LoadDetailTables = true;

    /// <summary>
    /// List of parameters when executing table valued function or stored procedure.
    /// </summary>
    protected internal List<StormCallParameter>? CallParameters;

    /// <summary>
    /// Table hints for a Storm query.
    /// </summary>
    protected internal StormTableHints TableHints;

    /// <summary>
    /// Retrieves the controller of type T from the StormControllerCache.
    /// </summary>
    /// <returns>The controller of type T.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual StormControllerBase GetController() => StormControllerCache.Get<T>(Variant);
}
