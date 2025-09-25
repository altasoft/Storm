using System;

namespace AltaSoft.Storm.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a primary key violation occurs in StormDb.
/// </summary>
public sealed class StormPrimaryKeyViolationException : StormDbException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StormPrimaryKeyViolationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message for the exception</param>
    /// <param name="innerException">The inner exception that caused this exception</param>
    public StormPrimaryKeyViolationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
