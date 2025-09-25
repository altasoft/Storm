using System;

namespace AltaSoft.Storm.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a unique key violation occurs in the Storm database.
/// </summary>
public sealed class StormUniqueKeyViolationException : StormDbException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StormUniqueKeyViolationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message for the exception</param>
    /// <param name="innerException">The inner exception that caused this exception</param>
    public StormUniqueKeyViolationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
