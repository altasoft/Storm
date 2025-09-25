namespace AltaSoft.Storm.Models;

public enum StormPropertyStatus
{
    NotChecked,
    Ok,
    NullableMismatch,
    KeyMismatch,
    DbTypePartiallyCompatible,
    SizeMismatch,
    PrecisionMismatch,
    ScaleMismatch,
    DbTypeNotCompatible,
    DetailTableNotFound,
    ColumnMissing
}
