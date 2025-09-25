using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Extensions;

namespace AltaSoft.Storm.Crud;

internal sealed class ExecuteProc<TResult> : ScalarParameters, IExecuteProc<TResult> where TResult : StormProcedureResult
{
    private readonly Func<int, StormDbParameterCollection, Exception?, TResult> _resultReader;

    /// <summary>
    /// Constructor for initializing a ExecuteProc object with a given DbConnection.
    /// </summary>
    public ExecuteProc(StormContext context, List<StormCallParameter> callParameters, string? schemaName, string objectName, Func<int, StormDbParameterCollection, Exception?, TResult> resultReader)
        : base(context, callParameters, schemaName, objectName)
    {
        _resultReader = resultReader;
    }

    public IExecuteProc<TResult> WithCloseConnection()
    {
        CloseConnection = true;
        return this;
    }

    public IExecuteProc<TResult> WithCommandTimeOut(int commandTimeout)
    {
        CommandTimeout = commandTimeout;
        return this;
    }

    public Task<TResult> ExecuteAsync(CancellationToken cancellationToken = default)
        => Context.ExecuteProcedureAsync(this, _resultReader, cancellationToken);
}
