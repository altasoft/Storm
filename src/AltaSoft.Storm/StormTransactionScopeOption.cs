namespace AltaSoft.Storm;

/// <summary>
/// Options that control how a <see cref="StormTransactionScope"/> participates in ambient transactions.
/// </summary>
public enum StormTransactionScopeOption
{
    /// <summary>A transaction is required by the scope. It uses an ambient transaction if one already exists. Otherwise, it creates a new transaction before entering the scope. This is the default value.</summary>
    Required,

    /// <summary>A new transaction is always created for the scope.</summary>
    RequiresNew,

    /// <summary>The ambient transaction context is suppressed when creating the scope. All operations within the scope are done without an ambient transaction context.</summary>
    Suppress
}
