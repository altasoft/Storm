// ReSharper disable ConvertToPrimaryConstructor

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Represents parameters specific to query operations in ORM (Object-Relational Mapping).
/// </summary>
public class QueryParameters
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryParameters"/> class with the specified Storm context.
    /// </summary>
    /// <param name="context">The Storm context.</param>
    public QueryParameters(StormContext context)
    {
        Context = context;
    }

    /// <summary>
    /// Storm context that contains the database connection and other settings.
    /// </summary>
    internal StormContext Context { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the database connection should be closed after the query execution.
    /// When set to true, the connection will be closed after the operation.
    /// This is useful for managing database connection lifetimes in scenarios where connections are not managed automatically.
    /// </summary>
    public bool CloseConnection { get; set; }

    /// <summary>
    /// Specifies the wait time (in seconds) before terminating the attempt to execute a command and generating an error.
    /// This property can be used to set a custom command timeout duration.
    /// A null value indicates that the default timeout for the database provider will be used.
    /// </summary>
    public int? CommandTimeout { get; set; }
}
