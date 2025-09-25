using System;

namespace AltaSoft.Storm.Exceptions;

/// <summary>
/// Represents errors that occur during ORM (Object-Relational Mapping) operations.
/// This class provides a specialized exception type for ORM-related issues.
/// </summary>
/// <remarks>
/// This exception should be used to signal problems specifically related to ORM operations,
/// such as issues with mapping, querying, or managing data entities.
/// </remarks>
public class StormException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StormException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message for the exception</param>
    public StormException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StormException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message for the exception</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public StormException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
