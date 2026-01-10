using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm;

/// <summary>
/// Abstract base class for ORM (Object-Relational Mapping) controllers, providing common functionality for database operations.
/// </summary>
public abstract partial class StormControllerBase
{
    #region Execute

    /// <summary>
    /// Streams data asynchronously from a database connection.
    /// </summary>
    /// <typeparam name="T">The type of data to stream.</typeparam>
    /// <typeparam name="TResult">The type of result data</typeparam>
    /// <param name="queryParameters">The Query parameters.</param>
    /// <param name="output">Specifies instance, where to output results of Stored Procedure execution.</param>
    /// <param name="outputWriter">Action that populates output</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable of the streamed data.</returns>
    internal async IAsyncEnumerable<T> ExecuteStreamAsync<T, TResult>(
        SelectQueryParameters<T> queryParameters,
        TResult? output,
        Action<int, StormDbParameterCollection, TResult> outputWriter,
        [EnumeratorCancellation] CancellationToken cancellationToken) where T : IDataBindable where TResult : StormProcedureResult
    {
        var command = StormManager.CreateCommand(true);
        await using (command.ConfigureAwait(false))
        {
            var vCommand = new StormVirtualDbCommand(command);
            var commandBehavior = PrepareGenerateExec(vCommand, queryParameters, false);

            var (connection, transaction) = await queryParameters.Context.EnsureConnectionAndTransactionIsOpenAsync(cancellationToken).ConfigureAwait(false);
            command.SetStormCommandBaseParameters(connection, transaction);

            var reader = await command.ExecuteCommandReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                if (output is not null)
                    outputWriter(reader.RecordsAffected, command.Parameters, output);

                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var idx = 0;
                    var row = (T)Create(reader, uint.MaxValue, ref idx);

                    if (queryParameters.AutoStartChangeTracking && row is IChangeTrackable changeTrackable)
                        changeTrackable.StartChangeTracking();

                    yield return row;
                }
            }

            if (output is not null) // We need to close reader before populating output
                outputWriter(reader.RecordsAffected, command.Parameters, output);
        }
    }

    /// <summary>
    /// Retrieves a list of objects asynchronously from the database based on the provided parameters.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <typeparam name="TResult">The type of result data</typeparam>
    /// <param name="queryParameters">The Query parameters.</param>
    /// <param name="output">Specifies instance, where to output results of Stored Procedure execution.</param>
    /// <param name="outputWriter">Action that populates output</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A list of objects retrieved from the database.</returns>
    internal async Task<List<T>> ExecuteListAsync<T, TResult>(
        SelectQueryParameters<T> queryParameters,
        TResult? output,
        Action<int, StormDbParameterCollection, TResult> outputWriter,
        CancellationToken cancellationToken) where T : IDataBindable where TResult : StormProcedureResult
    {
        var command = StormManager.CreateCommand(true);
        await using (command.ConfigureAwait(false))
        {
            var result = new List<T>(16);

            var vCommand = new StormVirtualDbCommand(command);
            var commandBehavior = PrepareGenerateExec(vCommand, queryParameters, false);

            var (connection, transaction) = await queryParameters.Context.EnsureConnectionAndTransactionIsOpenAsync(cancellationToken).ConfigureAwait(false);
            command.SetStormCommandBaseParameters(connection, transaction);

            var reader = await command.ExecuteCommandReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                await using (reader.ConfigureAwait(false))
                {
                    if (output is not null)
                        outputWriter(reader.RecordsAffected, command.Parameters, output);

                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        var idx = 0;
                        var row = (T)Create(reader, uint.MaxValue, ref idx);

                        if (queryParameters.AutoStartChangeTracking && row is IChangeTrackable changeTrackable)
                            changeTrackable.StartChangeTracking();

                        result.Add(row);
                    }
                }
            }

            if (output is not null) // We need to close reader before populating output
                outputWriter(reader.RecordsAffected, command.Parameters, output);
            return result;
        }
    }

    /// <summary>
    /// Retrieves a first record from the database, or returns the default value if no record is found.
    /// </summary>
    /// <typeparam name="T">The type of the record to retrieve.</typeparam>
    /// <typeparam name="TResult">The type of result data</typeparam>
    /// <param name="queryParameters">The Query parameters.</param>
    /// <param name="output">Specifies instance, where to output results of Stored Procedure execution.</param>
    /// <param name="outputWriter">Action that populates output</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The retrieved record, or the default value if no record is found.</returns>
    internal async Task<T?> ExecuteFirstOrDefaultAsync<T, TResult>(
        SelectQueryParameters<T> queryParameters,
        TResult? output,
        Action<int, StormDbParameterCollection, TResult> outputWriter,
        CancellationToken cancellationToken) where T : IDataBindable where TResult : StormProcedureResult
    {
        var command = StormManager.CreateCommand(true);
        await using (command.ConfigureAwait(false))
        {
            T? row = default;

            var vCommand = new StormVirtualDbCommand(command);
            var commandBehavior = PrepareGenerateExec(vCommand, queryParameters, true);

            var (connection, transaction) = await queryParameters.Context.EnsureConnectionAndTransactionIsOpenAsync(cancellationToken).ConfigureAwait(false);
            command.SetStormCommandBaseParameters(connection, transaction);

            var reader = await command.ExecuteCommandReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false))
            {
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var idx = 0;
                    row = (T)Create(reader, uint.MaxValue, ref idx);

                    if (queryParameters.AutoStartChangeTracking && row is IChangeTrackable changeTrackable)
                        changeTrackable.StartChangeTracking();
                }
            }

            if (output is not null) // We need to close reader before populating output
                outputWriter(reader.RecordsAffected, command.Parameters, output);
            return row;
        }
    }

    private CommandBehavior PrepareGenerateExec<T>(
        StormVirtualDbCommand command,
        SelectQueryParameters<T> queryParameters,
        bool getOnlyFirstRow) where T : IDataBindable
    {
        var callParameters = queryParameters.CallParameters;
        if (callParameters is not null)
            command.AddDbParameters(callParameters);

        command.SetStormCommandBaseParameters(QuotedObjectFullName, queryParameters, CommandType.StoredProcedure);

        var commandBehavior = CommandBehavior.SequentialAccess;
        commandBehavior |= CommandBehavior.SingleResult;
        if (getOnlyFirstRow)
            commandBehavior |= CommandBehavior.SingleRow;

        if (queryParameters.CloseConnection)
            commandBehavior |= CommandBehavior.CloseConnection;

        return commandBehavior;
    }

    #endregion Execute
}
