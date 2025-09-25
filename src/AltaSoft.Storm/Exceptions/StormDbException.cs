using System;

namespace AltaSoft.Storm.Exceptions;

/// <summary>
/// Represents errors that occur during ORM (Object-Relational Mapping) database operations.
/// This class provides a specialized exception type for database issues.
/// </summary>
/// <remarks>
/// This exception should be used to signal problems specifically related to database operations
/// </remarks>
public class StormDbException : StormException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StormDbException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message for the exception</param>
    /// <param name="innerException">The inner exception that caused this exception</param>
    public StormDbException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
