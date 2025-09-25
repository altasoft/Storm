using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using AltaSoft.Storm.Interfaces;
using Microsoft.Data.SqlClient;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Represents parameters specific to bulk insert in ORM (Object-Relational Mapping).
/// </summary>
internal abstract class BulkInsertQueryParameters<T> : QueryParameters where T : IDataBindable
{
    protected BulkInsertQueryParameters(StormContext context, int variant) : base(context)
    {
        Variant = variant;
    }

    /// <summary>
    /// Variant of the controller
    /// </summary>
    protected internal int Variant;

    /// <summary>
    /// Rows to insert
    /// </summary>
    protected IEnumerable<T>? RowValuesEnumerable;

    /// <summary>
    /// Rows to insert
    /// </summary>
    protected IAsyncEnumerable<T>? RowValuesAsyncEnumerable;

    /// <summary>
    /// Rows to insert
    /// </summary>
    protected ChannelReader<T>? RowValuesChannel;

    public SqlBulkCopyOptions BulkCopyOptions { get; set; } = SqlBulkCopyOptions.Default;

    public int? BatchSize { get; set; }

    public int NotifyAfter { get; set; }

    public Action<long>? ProgressNotification { get; set; }

    /// <summary>
    /// Retrieves the controller of type T from the StormControllerCache.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected StormControllerBase GetController() => StormControllerCache.Get<T>(Variant);
}
