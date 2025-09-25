namespace AltaSoft.Storm.Models;

public enum DbColumnStatus
{
    Ok,

    NullableMismatch,
    KeyMismatch,
    SizeMismatch,
    DbTypeMismatch,

    ColumnMissing
}
