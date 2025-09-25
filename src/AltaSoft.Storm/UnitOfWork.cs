using AltaSoft.Storm.Exceptions;
using Microsoft.Extensions.Logging;

namespace AltaSoft.Storm;

/// <summary>
/// Represents a unit of work for performing database operations within a transaction scope.
/// Handles transaction management, commit/rollback, and integration with an ambient unit of work context.
/// </summary>
public static class UnitOfWork
{
    /// <summary>
    /// Creates a new unit of work using the specified connection string.
    /// If no ambient unit of work is present, a new transaction and connection are created and owned by this instance.
    /// Otherwise, the unit of work participates in the existing ambient context.
    /// </summary>
    /// <returns>A new <see cref="IUnitOfWork"/> instance.</returns>
    /// <exception cref="StormException">
    /// Thrown if the provided connection string does not match the ambient unit of work connection.
    /// </exception>
    public static IUnitOfWork Create()
    {
        var logger = GetLogger();

        var ambientUow = AmbientUnitOfWork.Ambient;
        if (ambientUow is null)
        {
            ambientUow = new AmbientUnitOfWork(logger);
            logger?.LogTrace("[UnitOfWork] Created new root AmbientUnitOfWork instance.");
            return new UnitOfWorkInternal(ambientUow, true, logger);
        }

        logger?.LogTrace("[UnitOfWork] Joined existing AmbientUnitOfWork context.");
        return new UnitOfWorkInternal(ambientUow, false, logger);
    }

    /// <summary>
    /// Creates a new standalone unit of work, always creating a new ambient context.
    /// </summary>
    /// <returns>A new <see cref="IUnitOfWorkStandalone"/> instance that is always root.</returns>
    /// <exception cref="StormException">
    /// Thrown if the provided connection string does not match the ambient unit of work connection.
    /// </exception>
    public static IUnitOfWorkStandalone CreateStandalone()
    {
        var logger = GetLogger();

        var ambientUow = new AmbientUnitOfWork(logger);
        logger?.LogTrace("[UnitOfWork] Created new root AmbientUnitOfWork instance (Standalone).");
        return new UnitOfWorkInternal(ambientUow, true, logger);
    }

    /// <summary>
    /// Gets the logger instance if logging is enabled at the Trace level; otherwise, returns null.
    /// </summary>
    /// <returns>The <see cref="ILogger"/> instance or null.</returns>
    private static ILogger? GetLogger()
        => StormManager.Logger?.IsEnabled(LogLevel.Trace) == true ? StormManager.Logger : null;
}
