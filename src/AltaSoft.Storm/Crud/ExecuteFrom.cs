using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Interfaces;

// ReSharper disable ConvertToPrimaryConstructor

namespace AltaSoft.Storm.Crud;

internal sealed class ExecuteFrom<T, TOutput> : SelectQueryParameters<T>, IExecuteFrom<T, TOutput>
    where T : IDataBindable
    where TOutput : StormProcedureResult, new()
{
    private readonly Action<int, StormDbParameterCollection, TOutput> _outputWriter;
    private TOutput? _output;

    internal ExecuteFrom(StormContext context, int variant, List<StormCallParameter> callParameters, Action<int, StormDbParameterCollection, TOutput> outputWriter)
        : base(context, variant, callParameters)
    {
        _outputWriter = outputWriter;
    }

    #region Builder

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IExecuteFrom<T, TOutput> WithCloseConnection()
    {
        CloseConnection = true;
        return this;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IExecuteFrom<T, TOutput> WithCommandTimeOut(int commandTimeout)
    {
        CommandTimeout = commandTimeout;
        return this;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IExecuteFrom<T, TOutput> WithTracking()
    {
        AutoStartChangeTracking = true;
        return this;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IExecuteFrom<T, TOutput> WithNoTracking()
    {
        AutoStartChangeTracking = false;
        return this;
    }

    #endregion Builder

    /// <inheritdoc/>
    public IExecuteFrom<T, TOutput> OutputResultInto(out TOutput output)
    {
        _output = output = new TOutput();
        return this;
    }

    #region Get

    /// <inheritdoc/>
    public Task<T?> GetAsync(CancellationToken cancellationToken = default)
        => GetController().ExecuteFirstOrDefaultAsync(this, _output, _outputWriter, cancellationToken);

    #endregion Get

    #region List

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<List<T>> ListAsync(CancellationToken cancellationToken = default)
        => GetController().ExecuteListAsync(this, _output, _outputWriter, cancellationToken);

    #endregion List

    #region Stream

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IAsyncEnumerable<T> StreamAsync(CancellationToken cancellationToken = default)
        => GetController().ExecuteStreamAsync(this, _output, _outputWriter, cancellationToken);

    #endregion Stream
}
