using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm.BulkInsert;

internal sealed class ChannelDbDataReader<T> : StormDbDataReaderBase
    where T : IDataBindable
{
    internal readonly ChannelReader<T> _channelReader;

    internal ChannelDbDataReader(ChannelReader<T> channelReader, int fieldCount) : base(fieldCount)
    {
        _channelReader = channelReader;
    }

    /// <inheritdoc/>
    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        if (!_channelReader.TryRead(out var t))
            return Task.FromResult(false);
        CurrentColumnValues = t.__GetColumnValues();
        return Task.FromResult(true);
    }
}
