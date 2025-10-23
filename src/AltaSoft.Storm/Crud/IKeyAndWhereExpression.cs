using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Interface for defining key values and filtering expression for entities of type T.
/// </summary>
public interface IKeyAndWhereExpression<T> where T : IDataBindable
{
    /// <summary>
    /// Array of objects representing key values, can be null.
    /// </summary>
    internal object[]? KeyValues { get; }

    /// <summary>
    /// ID of the unique index/primary key in KeyColumnDefs array.
    /// </summary>
    int? KeyId { get; }

    /// <summary>
    /// Gets the expression used for filtering entities of type T.
    /// </summary>
    internal List<Expression<Func<T, bool>>>? WhereExpressions { get; }

    /// <summary>
    /// Gets the OData filter string used for filtering entities of type T.
    /// </summary>
    internal string? ODataFilter { get; }
}
