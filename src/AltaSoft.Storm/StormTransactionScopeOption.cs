namespace AltaSoft.Storm;

/// <summary>
/// Options that control how a <see cref="StormTransactionScope"/> participates in ambient transactions.
/// </summary>
public enum StormTransactionScopeOption
{
    /// <summary>
    /// Join an existing ambient transaction if one exists; otherwise create a new ambient transaction.
    /// </summary>
    JoinExisting,

    /// <summary>
    /// Always create a new ambient transaction scope that does not join an existing one.
    /// </summary>
    CreateNew
}
