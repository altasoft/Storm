using System;

namespace AltaSoft.Storm.Exceptions;

/// <summary>
/// Represents an exception that is thrown when a foreign key constraint is violated in Storm database operations.
/// </summary>
public sealed class StormForeignKeyViolationException : StormDbException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StormForeignKeyViolationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message for the exception</param>
    /// <param name="innerException">The inner exception that caused this exception</param>
    public StormForeignKeyViolationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
