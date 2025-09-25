using System;
using Microsoft.CodeAnalysis;

namespace AltaSoft.Storm.Generator.Common;

/// <summary>
/// Exception thrown when an invalid StormContext is specified.
/// </summary>
/// <remarks>
/// This exception is thrown when the provided type or any of its base types is not a descendent of StormContext,
/// indicating that an invalid StormContext was specified.
/// </remarks>
public sealed class InvalidStormAttributeParams : Exception
{
    public string Id { get; }

    public string Title { get; }

    public Location? Location { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidStormAttributeParams"/> class with a specified error message.
    /// </summary>
    /// <param name="id">The ID of the diagnostic.</param>
    /// <param name="title">The title of the diagnostic.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="location">Location of an error</param>
    internal InvalidStormAttributeParams(string id, string title, string message, Location? location) : base(message)
    {
        Id = id;
        Title = title;
        Location = location;
    }
}
