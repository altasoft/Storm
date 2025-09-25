using System;

namespace AltaSoft.Storm.Models;

internal sealed class ConnectionData
{
    public string ConnectionString { get; set; }
    public Guid Source { get; set; }
    public Guid Provider { get; set; }

    public ConnectionData(string connectionString, Guid source, Guid provider)
    {
        ConnectionString = connectionString;
        Source = source;
        Provider = provider;
    }
}
