using System;

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Represents a set of flags for specifying table hints for SQL queries.
/// </summary>
[Flags]
public enum StormTableHints
{
    /// <summary>
    /// No table hint specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Allows dirty reads by not issuing shared locks and not honoring exclusive locks.
    /// </summary>
    NoLock = 1 << 0,

    /// <summary>
    /// Same effect as NOLOCK.
    /// </summary>
    ReadUncommitted = 1 << 1,

    /// <summary>
    /// Holds update locks until the end of the transaction.
    /// </summary>
    UpdLock = 1 << 2,

    /// <summary>
    /// Holds a shared lock until the transaction is completed.
    /// </summary>
    HoldLock = 1 << 3,

    /// <summary>
    /// The default isolation level, which requests shared locks.
    /// </summary>
    ReadCommitted = 1 << 4,

    /// <summary>
    /// Holds shared locks until the transaction completes and thus prevents others from updating the rows.
    /// </summary>
    RepeatableRead = 1 << 5,

    /// <summary>
    /// Places a range lock on the data set, preventing other users from updating or inserting rows into the data set until the transaction is complete.
    /// </summary>
    Serializable = 1 << 6,

    /// <summary>
    /// Requests a lock on the table for the duration of the statement or transaction.
    /// </summary>
    TabLock = 1 << 7,

    /// <summary>
    /// Requests an exclusive lock on the table for the duration of the statement or transaction.
    /// </summary>
    TabLockX = 1 << 8,

    /// <summary>
    /// Requests a lock at the page level.
    /// </summary>
    PagLock = 1 << 9,

    /// <summary>
    /// Requests a row-level lock.
    /// </summary>
    RowLock = 1 << 10,

    /// <summary>
    /// Requests an exclusive lock as opposed to a shared lock.
    /// </summary>
    XLock = 1 << 11,

    /// <summary>
    /// Causes the statement to fail if the requested lock cannot be acquired immediately.
    /// </summary>
    NoWait = 1 << 12,

    /// <summary>
    /// Forces the use of an index seek operation.
    /// </summary>
    ForceSeek = 1 << 13,

    /// <summary>
    /// Forces the use of a scan operation.
    /// </summary>
    ForceScan = 1 << 14,

    /// <summary>
    /// Disables the checking of constraints.
    /// </summary>
    IgnoreConstraints = 1 << 15,

    /// <summary>
    /// Disables the firing of triggers.
    /// </summary>
    IgnoreTriggers = 1 << 16,

    /// <summary>
    /// Used in INSERT INTO SELECT statements to keep the source identity values.
    /// </summary>
    KeepIdentity = 1 << 17,

    /// <summary>
    /// Used in INSERT INTO SELECT statements to keep the default values of the table.
    /// </summary>
    KeepDefaults = 1 << 18,

    /// <summary>
    /// Specifies that additional rows with matching values in the ORDER BY columns are returned when the ROW or PERCENT limits are reached.
    /// </summary>
    Ties = 1 << 19,

    /// <summary>
    /// Disables automatic expansion of indexed views.
    /// </summary>
    NoExpand = 1 << 20,

    /// <summary>
    /// Skips locked rows.
    /// </summary>
    ReadPast = 1 << 21
}
