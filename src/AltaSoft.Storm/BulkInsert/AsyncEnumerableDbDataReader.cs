using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.BulkInsert;

internal sealed class AsyncEnumerableDbDataReader<T> : StormDbDataReaderBase where T : IDataBindable
{
    private readonly IAsyncEnumerable<T> _values;
    private IAsyncEnumerator<T>? _enumerator;

    public AsyncEnumerableDbDataReader(IAsyncEnumerable<T> values, int fieldCount) : base(fieldCount)
    {
        _values = values;
    }

    /// <inheritdoc/>
    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        _enumerator ??= _values.GetAsyncEnumerator(cancellationToken);

        // MoveNextAsync will throw if cancellation is requested.
        if (!await _enumerator.MoveNextAsync().ConfigureAwait(false))
            return false;

        var t = _enumerator.Current;

        CurrentColumnValues = t.__GetColumnValues();
        return true;
    }

    public override async ValueTask DisposeAsync()
    {
        if (_enumerator is not null)
        {
            await _enumerator.DisposeAsync().ConfigureAwait(false);
            _enumerator = null;
        }
        await base.DisposeAsync().ConfigureAwait(false);
    }
}
