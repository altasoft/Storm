namespace AltaSoft.Storm.Attributes;

/// <summary>
/// Specifies how a property should be saved in the database.
/// </summary>
public enum SaveAs
{
    /// <summary>
    /// Saves the property as a value for well-known types, otherwise ignores it.
    /// This is the default behavior and is typically used for basic data types like integers, strings, etc.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Saves the property as a plain string.
    /// This option is useful for storing enums as text.
    /// </summary>
    String = 1,

    /// <summary>
    /// Saves the property as a string compressed with default compression algorithm.
    /// This option is useful for storing large text data in a compressed format.
    /// </summary>
    CompressedString = 2,

    /// <summary>
    /// Saves the property as a serialized Json text string.
    /// This option is useful for storing complex objects as a single string in the database.
    /// </summary>
    Json = 10,

    /// <summary>
    /// Saves the property as a serialized Json text compressed with default compression algorithm.
    /// This option is useful for storing complex objects as a single binary in the database.
    /// </summary>
    CompressedJson = 11,

    /// <summary>
    /// Saves the property as a serialized Xml text string.
    /// This option is useful for storing complex objects as a single string in the database.
    /// </summary>
    Xml = 20,

    /// <summary>
    /// Saves the property as a serialized Xml text compressed with default compression algorithm.
    /// This option is useful for storing complex objects as a single binary in the database.
    /// </summary>
    CompressedXml = 21,

    /// <summary>
    /// Flattens the object and saves it as separate columns in the database.
    /// This option is used for complex types where each property of the object is saved in its own column.
    /// </summary>
    FlatObject = 30,

    /// <summary>
    /// Saves the property in a Master/Details detail table.
    /// This is typically used for relationships where the property represents a collection of items that are stored in a separate table.
    /// </summary>
    DetailTable = 40,

    /// <summary>
    /// Ignores the property and does not save it to the database.
    /// This is useful for properties that should not be persisted.
    /// </summary>
    Ignore = 99 // Note: Do not change the value of this enum member
}
