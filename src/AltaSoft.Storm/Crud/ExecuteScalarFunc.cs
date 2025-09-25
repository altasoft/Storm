using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Extensions;

namespace AltaSoft.Storm.Crud;

internal sealed class ExecuteScalarFunc<TResult> : ScalarParameters, IExecuteScalarFunc<TResult>
{
    private readonly Func<StormDbDataReader, TResult> _resultReader;

    /// <summary>
    /// Constructor for initializing a ExecuteScalarFunc object with a given DbConnection.
    /// </summary>
    public ExecuteScalarFunc(StormContext context, List<StormCallParameter> callParameters, string? schemaName, string objectName, Func<StormDbDataReader, TResult> resultReader)
        : base(context, callParameters, schemaName, objectName)
    {
        _resultReader = resultReader;
    }

    public IExecuteScalarFunc<TResult> WithCloseConnection()
    {
        CloseConnection = true;
        return this;
    }

    public IExecuteScalarFunc<TResult> WithCommandTimeOut(int commandTimeout)
    {
        CommandTimeout = commandTimeout;
        return this;
    }

    public Task<TResult> GetAsync(CancellationToken cancellationToken = default) => Context.ExecuteScalarAsync(this, _resultReader, cancellationToken);
}
