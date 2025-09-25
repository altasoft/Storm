using System;

namespace AltaSoft.Storm.Exceptions;

/// <summary>
/// Represents an exception that is thrown when there is a concurrency issue in a database.
/// </summary>
public sealed class StormDbConcurrencyException : StormDbException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StormDbConcurrencyException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message for the exception</param>
    /// <param name="innerException">The inner exception that caused this exception</param>
    public StormDbConcurrencyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
