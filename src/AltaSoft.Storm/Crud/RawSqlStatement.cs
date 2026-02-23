//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using AltaSoft.Storm.Extensions;

//namespace AltaSoft.Storm.Crud;

///// <summary>
///// Represents a raw SQL statement execution request. Encapsulates the SQL statement and
///// provides methods to execute it directly or to generate batch commands.
///// </summary>
//internal sealed class RawSqlStatement : QueryParameters, IRawSqlStatement
//{
//    /// <summary>
//    /// The SQL statement to be executed.
//    /// </summary>
//    public string Statement { get; }

//    /// <summary>
//    /// Initializes a new instance of the <see cref="RawSqlStatement"/> class with the provided
//    /// <paramref name="context"/> and SQL statement text.
//    /// </summary>
//    /// <param name="context">The <see cref="StormContext"/> used for execution.</param>
//    /// <param name="sqlStatement">The SQL statement text to execute.</param>
//    public RawSqlStatement(StormContext context, string sqlStatement) : base(context)
//    {
//        Statement = sqlStatement;
//    }

//    /// <inheritdoc/>
//    public async Task<int> GoAsync(CancellationToken cancellationToken = default)
//    {
//        var command = StormManager.CreateCommand(false);
//        await using (command.ConfigureAwait(false))
//        {
//            // Set up the command text and parameters for execution.
//            command.SetStormCommandBaseParameters(Statement, this);

//            var (connection, transaction) = await Context.EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
//            command.SetStormCommandBaseParameters(connection, transaction);

//            return await command.ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
//        }
//    }

//    /// <inheritdoc/>
//    public void GenerateBatchCommands(List<StormDbBatchCommand> batchCommands)
//    {
//        var command = StormManager.CreateBatchCommand(false);
//        command.SetStormCommandBaseParameters(Statement);
//        batchCommands.Add(command);
//    }
//}
