using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Base class for deleting data from a database table
/// </summary>
internal class InsertInto<T> : ModifyQueryParameters<T>, IInsertInto<T> where T : IDataBindable
{
    /// <summary>
    /// Constructor for initializing a InsertInto object with a given DbConnection.
    /// </summary>
    internal InsertInto(StormContext context, int variant, string? customQuotedObjectFullName = null) : base(context, variant, customQuotedObjectFullName)
    {
    }

    #region Builder

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IInsertInto<T> WithCloseConnection()
    {
        CloseConnection = true;
        return this;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IInsertInto<T> WithCommandTimeOut(int commandTimeout)
    {
        CommandTimeout = commandTimeout;
        return this;
    }

    #endregion Builder

    /// <inheritdoc/>
    public ISqlGo Values(T value)
    {
        RowValue = value;
        return this;
    }

    /// <inheritdoc/>
    public ISqlGo Values(IEnumerable<T> values)
    {
        RowValues = values;
        return this;
    }

    /// <inheritdoc/>
    public Task<int> GoAsync(CancellationToken cancellationToken = default)
    {
        if (RowValue is not null)
            return GetController().InsertAsync(RowValue, this, cancellationToken);

        if (RowValues is not null)
            return GetController().InsertAsync(RowValues, this, cancellationToken);

        throw new StormException("No values to insert."); // This will never happen
    }

    /// <param name="batchCommands"></param>
    /// <inheritdoc />
    public void GenerateBatchCommands(List<StormDbBatchCommand> batchCommands)
    {
        if (RowValue is not null)
        {
            var command = StormManager.CreateBatchCommand(false);
            var vCommand = new StormVirtualDbBatchCommand(command);

            GetController().Insert(vCommand, RowValue, this);
            batchCommands.Add(command);
            return;
        }

        if (RowValues is not null)
        {
            var command = StormManager.CreateBatchCommand(false);
            var vCommand = new StormVirtualDbBatchCommand(command);

            GetController().Insert(vCommand, RowValues, this);
            batchCommands.Add(command);
        }
    }
}
