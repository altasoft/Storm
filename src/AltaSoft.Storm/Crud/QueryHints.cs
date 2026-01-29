using System;
using System.Collections.Generic;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Hints related to query compilation and plan selection.
/// </summary>
public sealed record QueryHints
{
    /// <summary>
    /// Adds plan-related hints. Use <see cref="QueryPlanHint.Recompile"/> to force recompilation
    /// or <see cref="QueryPlanHint.OptimizeForUnknown"/> to use the <c>OPTIMIZE FOR UNKNOWN</c> behavior.
    /// </summary>
    public QueryPlanHint? Plan { get; init; }

    /// <summary>
    /// Specifies the maximum degree of parallelism for the query. Corresponds to <c>MAXDOP n</c>.
    /// When <c>null</c> the server default is used.
    /// </summary>
    public int? MaxDop { get; init; }

    /// <summary>
    /// Hint used to instruct the optimizer to favor returning the first
    /// <c>n</c> rows as quickly as possible. Only applied when positive.
    /// Corresponds to the <c>FAST n</c> option.
    /// </summary>
    public int? Fast { get; init; }

    /// <summary>
    /// Specifies join strategy hint for the optimizer (LOOP, HASH or MERGE join). Rendered as
    /// <c>LOOP JOIN</c>, <c>HASH JOIN</c> or <c>MERGE JOIN</c>.
    /// </summary>
    public QueryJoinHint? Join { get; init; }

    /// <summary>
    /// When <c>true</c>, enforces join ordering specified in the query using <c>FORCE ORDER</c>.
    /// </summary>
    public bool? ForceOrder { get; init; }

    /// <summary>
    /// Enables tracing flags using <c>QUERYTRACEON n</c>. Use with caution; behavior is server-specific.
    /// </summary>
    public int? QueryTraceOn { get; init; }

    /// <summary>
    /// When <c>true</c>, includes the <c>KEEP PLAN</c> hint which suggests the optimizer keep the
    /// chosen plan for subsequent executions. Support for this hint depends on SQL Server version.
    /// </summary>
    public bool KeepPlan { get; init; }

    /// <summary>
    /// Converts the set of <see cref="QueryHints"/> into a list of SQL hint fragments suitable
    /// for inclusion in a query hint clause. Returns <c>null</c> when no hints are specified.
    /// </summary>
    /// <returns>A list of SQL hint strings or <c>null</c> if none are set.</returns>
    public List<string>? ToHintsList()
    {
        List<string>? hints = null;

        if (Plan is not null)
        {
            Add(Plan switch
            {
                QueryPlanHint.Recompile => "RECOMPILE",
                QueryPlanHint.OptimizeForUnknown => "OPTIMIZE FOR UNKNOWN",
                _ => throw new InvalidOperationException($"Unexpected QueryPlanHint value: {Plan}")
            });
        }

        if (MaxDop is > 0)
            Add($"MAXDOP {MaxDop}");

        if (Fast is > 0)
            Add($"FAST {Fast}");

        if (ForceOrder == true)
            Add("FORCE ORDER");

        if (Join is not null)
        {
            Add(Join switch
            {
                QueryJoinHint.Loop => "LOOP JOIN",
                QueryJoinHint.Hash => "HASH JOIN",
                QueryJoinHint.Merge => "MERGE JOIN",
                _ => throw new InvalidOperationException($"Unexpected QueryJoinHint value: {Join}")
            });
        }

        if (QueryTraceOn is > 0)
            Add($"QUERYTRACEON {QueryTraceOn}");

        if (KeepPlan)
            Add("KEEP PLAN");

        return hints;

        void Add(string value)
        {
            hints ??= new List<string>(8);
            hints.Add(value);
        }
    }
}

/// <summary>
/// Plan-related hint options that control compilation and cardinality estimation behavior.
/// </summary>
public enum QueryPlanHint
{
    /// <summary>
    /// Instructs the optimizer to recompile the query each time it's executed (useful for parameter-sensitive plans).
    /// </summary>
    Recompile,

    /// <summary>
    /// Instructs the optimizer to treat all local variables as unknown for cardinality estimation.
    /// This renders as <c>OPTIMIZE FOR UNKNOWN</c>.
    /// </summary>
    OptimizeForUnknown
}

/// <summary>
/// Join algorithm hints that suggest which join operator the optimizer should prefer.
/// </summary>
public enum QueryJoinHint
{
    /// <summary>
    /// Prefer nested loop join algorithm.
    /// </summary>
    Loop,

    /// <summary>
    /// Prefer hash join algorithm.
    /// </summary>
    Hash,

    /// <summary>
    /// Prefer merge join algorithm.
    /// </summary>
    Merge
}
