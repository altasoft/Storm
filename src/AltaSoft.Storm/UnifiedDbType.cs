namespace AltaSoft.Storm;

/// <summary>
/// Represents a unified set of data types encompassing various database systems and .NET types.
/// This unified type system facilitates the handling of diverse data types across different databases and application layers.
/// </summary>
public enum UnifiedDbType
{
    /// <summary>
    /// The default placeholder value, used to represent an unspecified data type.
    /// This value can be used as a fallback when a specific type is not necessary or has not been determined.
    /// </summary>
    Default = 0,

    /// <summary>
    /// A simple type representing Boolean values of <see langword="true" /> or <see langword="false" />.
    /// <list type="bullet">
    /// <item><description>SQL Server: Bit</description></item>
    /// <item><description>PostgreSQL: Boolean</description></item>
    /// <item><description>MySQL: TinyInt(1)</description></item>
    /// <item><description>SQLite: INTEGER</description></item>
    /// <item><description>Oracle: NUMBER(1)</description></item>
    /// </list>
    /// </summary>
    Boolean = 1,

    /// <summary>
    /// An 8-bit unsigned integer ranging in value from 0 to 255.
    /// <list type="bullet">
    /// <item><description>SQL Server: TinyInt</description></item>
    /// <item><description>PostgreSQL: SmallInt</description></item>
    /// <item><description>MySQL: Unsigned TinyInt</description></item>
    /// <item><description>SQLite: INTEGER</description></item>
    /// <item><description>Oracle: NUMBER(3)</description></item>
    /// </list>
    /// </summary>
    UInt8,

    /// <summary>
    /// An integral type representing signed 8-bit integers with values between -128 and 127.
    /// <list type="bullet">
    /// <item><description>SQL Server: SmallInt</description></item>
    /// <item><description>PostgreSQL: "char"</description></item>
    /// <item><description>MySQL: TinyInt</description></item>
    /// <item><description>SQLite: INTEGER</description></item>
    /// <item><description>Oracle: NUMBER(3)</description></item>
    /// </list>
    /// </summary>
    Int8,

    /// <summary>
    /// An integral type representing unsigned 16-bit integers with values between 0 and 65535.
    /// <list type="bullet">
    /// <item><description>SQL Server: Int</description></item>
    /// <item><description>PostgreSQL: Integer</description></item>
    /// <item><description>MySQL: Unsigned SmallInt</description></item>
    /// <item><description>SQLite: INTEGER</description></item>
    /// <item><description>Oracle: NUMBER(5)</description></item>
    /// </list>
    /// </summary>
    UInt16,

    /// <summary>
    /// An integral type representing signed 16-bit integers with values between -32768 and 32767.
    /// <list type="bullet">
    /// <item><description>SQL Server: SmallInt</description></item>
    /// <item><description>PostgreSQL: SmallInt</description></item>
    /// <item><description>MySQL: SmallInt</description></item>
    /// <item><description>SQLite: INTEGER</description></item>
    /// <item><description>Oracle: NUMBER(5)</description></item>
    /// </list>
    /// </summary>
    Int16,

    /// <summary>
    /// An integral type representing unsigned 32-bit integers with values between 0 and 4294967295.
    /// <list type="bullet">
    /// <item><description>SQL Server: Bigint</description></item>
    /// <item><description>PostgreSQL: Bigint</description></item>
    /// <item><description>MySQL: Unsigned Int</description></item>
    /// <item><description>SQLite: INTEGER</description></item>
    /// <item><description>Oracle: NUMBER(10)</description></item>
    /// </list>
    /// </summary>
    UInt32,

    /// <summary>
    /// An integral type representing signed 32-bit integers with values between -2147483648 and 2147483647.
    /// <list type="bullet">
    /// <item><description>SQL Server: Int</description></item>
    /// <item><description>PostgreSQL: Integer</description></item>
    /// <item><description>MySQL: Int</description></item>
    /// <item><description>SQLite: INTEGER</description></item>
    /// <item><description>Oracle: NUMBER(10)</description></item>
    /// </list>
    /// </summary>
    Int32,

    /// <summary>
    /// An integral type representing unsigned 64-bit integers with values between 0 and 18446744073709551615.
    /// <list type="bullet">
    /// <item><description>SQL Server: Not supported</description></item>
    /// <item><description>PostgreSQL: Numeric</description></item>
    /// <item><description>MySQL: Unsigned Bigint</description></item>
    /// <item><description>SQLite: Not supported</description></item>
    /// <item><description>Oracle: Not supported</description></item>
    /// </list>
    /// </summary>
    UInt64,

    /// <summary>
    /// An integral type representing signed 64-bit integers with values between -9223372036854775808 and 9223372036854775807.
    /// <list type="bullet">
    /// <item><description>SQL Server: Bigint</description></item>
    /// <item><description>PostgreSQL: Bigint</description></item>
    /// <item><description>MySQL: Bigint</description></item>
    /// <item><description>SQLite: INTEGER</description></item>
    /// <item><description>Oracle: NUMBER(19)</description></item>
    /// </list>
    /// </summary>
    Int64,

    /// <summary>
    /// A variable-length stream of non-Unicode characters ranging between 1 and 8,000 characters.
    /// <list type="bullet">
    /// <item><description>SQL Server: VarChar</description></item>
    /// <item><description>PostgreSQL: Varchar</description></item>
    /// <item><description>MySQL: VarChar</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: VARCHAR2</description></item>
    /// </list>
    /// </summary>
    AnsiString,

    /// <summary>
    /// A variable-length stream of Unicode characters ranging between 1 and 4,000 characters.
    /// <list type="bullet">
    /// <item><description>SQL Server: NVarChar</description></item>
    /// <item><description>PostgreSQL: Text</description></item>
    /// <item><description>MySQL: VarChar</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: NVARCHAR2</description></item>
    /// </list>
    /// </summary>
    String,

    /// <summary>
    /// A fixed-length stream of non-Unicode characters.
    /// <list type="bullet">
    /// <item><description>SQL Server: Char</description></item>
    /// <item><description>PostgreSQL: Char</description></item>
    /// <item><description>MySQL: Char</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: CHAR</description></item>
    /// </list>
    /// </summary>
    AnsiStringFixedLength,

    /// <summary>
    /// A fixed-length string of Unicode characters.
    /// <list type="bullet">
    /// <item><description>SQL Server: NChar</description></item>
    /// <item><description>PostgreSQL: Char</description></item>
    /// <item><description>MySQL: Char</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: NCHAR</description></item>
    /// </list>
    /// </summary>
    StringFixedLength,

    ///// <summary>
    ///// A variable-length stream of binary data representing compressed string.
    ///// <list type="bullet">
    ///// <item><description>SQL Server: Binary</description></item>
    ///// <item><description>PostgreSQL: Bytea</description></item>
    ///// <item><description>MySQL: Blob</description></item>
    ///// <item><description>SQLite: BLOB</description></item>
    ///// <item><description>Oracle: BLOB</description></item>
    ///// </list>
    ///// Ranging between 1 and 8,000 bytes.
    ///// </summary>
    //CompressedString,

    /// <summary>
    /// A currency value ranging from -2^63 (or -922,337,203,685,477.5808) to 2^63 -1 (or +922,337,203,685,477.5807) with an accuracy to a ten-thousandth of a currency unit.
    /// <list type="bullet">
    /// <item><description>SQL Server: Money</description></item>
    /// <item><description>PostgreSQL: Money</description></item>
    /// <item><description>MySQL: Decimal</description></item>
    /// <item><description>SQLite: NUMERIC</description></item>
    /// <item><description>Oracle: NUMBER</description></item>
    /// </list>
    /// </summary>
    Currency = 200,

    /// <summary>
    /// A floating point type representing values ranging from approximately 1.5 x 10^-45 to 3.4 x 10^38 with a precision of 7 digits.
    /// <list type="bullet">
    /// <item><description>SQL Server: Real</description></item>
    /// <item><description>PostgreSQL: Real</description></item>
    /// <item><description>MySQL: Float</description></item>
    /// <item><description>SQLite: REAL</description></item>
    /// <item><description>Oracle: BINARY_FLOAT</description></item>
    /// </list>
    /// </summary>
    Single,

    /// <summary>
    /// A floating point type representing values ranging from approximately 5.0 x 10^-324 to 1.7 x 10^308 with a precision of 15-16 digits.
    /// <list type="bullet">
    /// <item><description>SQL Server: Float</description></item>
    /// <item><description>PostgreSQL: Double Precision</description></item>
    /// <item><description>MySQL: Double</description></item>
    /// <item><description>SQLite: REAL</description></item>
    /// <item><description>Oracle: BINARY_DOUBLE</description></item>
    /// </list>
    /// </summary>
    Double,

    /// <summary>
    /// A simple type representing values ranging from 1.0 x 10^-28 to approximately 7.9 x 10^28 with 28-29 significant digits.
    /// <list type="bullet">
    /// <item><description>SQL Server: Decimal</description></item>
    /// <item><description>PostgreSQL: Numeric</description></item>
    /// <item><description>MySQL: Decimal</description></item>
    /// <item><description>SQLite: NUMERIC</description></item>
    /// <item><description>Oracle: NUMBER</description></item>
    /// </list>
    /// </summary>
    Decimal,

    ///// <summary>
    ///// A variable-length numeric value.
    ///// <list type="bullet">
    ///// <item><description>SQL Server: Numeric</description></item>
    ///// <item><description>PostgreSQL: Numeric</description></item>
    ///// <item><description>MySQL: Decimal</description></item>
    ///// <item><description>SQLite: NUMERIC</description></item>
    ///// <item><description>Oracle: NUMBER</description></item>
    ///// </list>
    ///// </summary>
    //VarNumeric,

    /// <summary>
    /// A type representing a date and time value.
    /// <list type="bullet">
    /// <item><description>SQL Server: SmallDateTime</description></item>
    /// <item><description>PostgreSQL: Timestamp</description></item>
    /// <item><description>MySQL: DateTime</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: DATE</description></item>
    /// </list>
    /// </summary>
    SmallDateTime = 300,

    /// <summary>
    /// Date and time data. Date value range is from January 1,1 AD through December 31, 9999 AD. Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy of 100 nanoseconds.
    /// <list type="bullet">
    /// <item><description>SQL Server: DateTime</description></item>
    /// <item><description>PostgreSQL: Timestamp</description></item>
    /// <item><description>MySQL: DateTime</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: TIMESTAMP</description></item>
    /// </list>
    /// </summary>
    DateTime,

    /// <summary>
    /// Date and time data. Date value range is from January 1,1 AD through December 31, 9999 AD. Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy of 100 nanoseconds.
    /// <list type="bullet">
    /// <item><description>SQL Server: DateTime2</description></item>
    /// <item><description>PostgreSQL: Timestamp</description></item>
    /// <item><description>MySQL: DateTime</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: TIMESTAMP</description></item>
    /// </list>
    /// </summary>
    DateTime2,

    /// <summary>
    /// Date and time data with time zone awareness. Date value range is from January 1,1 AD through December 31, 9999 AD. Time value range is 00:00:00 through 23:59:59.9999999 with an accuracy of 100 nanoseconds. Time zone value range is -14:00 through +14:00.
    /// <list type="bullet">
    /// <item><description>SQL Server: DateTimeOffset</description></item>
    /// <item><description>PostgreSQL: Timestamptz</description></item>
    /// <item><description>MySQL: DateTime</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: TIMESTAMP WITH TIME ZONE</description></item>
    /// </list>
    /// </summary>
    DateTimeOffset,

    /// <summary>
    /// A type representing a date value.
    /// <list type="bullet">
    /// <item><description>SQL Server: Date</description></item>
    /// <item><description>PostgreSQL: Date</description></item>
    /// <item><description>MySQL: Date</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: DATE</description></item>
    /// </list>
    /// </summary>
    Date,

    /// <summary>
    /// A type representing a time value.
    /// <list type="bullet">
    /// <item><description>SQL Server: Time</description></item>
    /// <item><description>PostgreSQL: Time</description></item>
    /// <item><description>MySQL: Time</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: DATE</description></item>
    /// </list>
    /// </summary>
    Time,

    /// <summary>
    /// A globally unique identifier (or GUID).
    /// <list type="bullet">
    /// <item><description>SQL Server: UniqueIdentifier</description></item>
    /// <item><description>PostgreSQL: Uuid</description></item>
    /// <item><description>MySQL: Char(36)</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: RAW(16)</description></item>
    /// </list>
    /// </summary>
    Guid = 400,

    /// <summary>
    /// A parsed representation of an XML document or fragment.
    /// <list type="bullet">
    /// <item><description>SQL Server: Xml</description></item>
    /// <item><description>PostgreSQL: Xml</description></item>
    /// <item><description>MySQL: Text</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: XMLType</description></item>
    /// </list>
    /// </summary>
    AnsiXml,

    /// <summary>
    /// A parsed representation of an XML document or fragment.
    /// <list type="bullet">
    /// <item><description>SQL Server: Xml</description></item>
    /// <item><description>PostgreSQL: Xml</description></item>
    /// <item><description>MySQL: Text</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: XMLType</description></item>
    /// </list>
    /// </summary>
    Xml,

    /// <summary>
    /// A parsed representation of a non-Unicode Json document.
    /// <list type="bullet">
    /// <item><description>SQL Server: VarChar</description></item>
    /// <item><description>PostgreSQL: Json</description></item>
    /// <item><description>MySQL: Json</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: CLOB</description></item>
    /// </list>
    /// </summary>
    AnsiJson,

    /// <summary>
    /// A parsed representation of a Unicode Json document.
    /// <list type="bullet">
    /// <item><description>SQL Server: NVarChar</description></item>
    /// <item><description>PostgreSQL: Json</description></item>
    /// <item><description>MySQL: Json</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: CLOB</description></item>
    /// </list>
    /// </summary>
    Json,

    /// <summary>
    /// A variable-length stream of non-Unicode characters.
    /// <list type="bullet">
    /// <item><description>SQL Server: Text</description></item>
    /// <item><description>PostgreSQL: Text</description></item>
    /// <item><description>MySQL: Text</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: CLOB</description></item>
    /// </list>
    /// This type is typically used for large text data that doesn't require Unicode encoding.
    /// </summary>
    AnsiText,

    /// <summary>
    /// A variable-length stream of Unicode characters.
    /// <list type="bullet">
    /// <item><description>SQL Server: NText</description></item>
    /// <item><description>PostgreSQL: Text</description></item>
    /// <item><description>MySQL: Text</description></item>
    /// <item><description>SQLite: TEXT</description></item>
    /// <item><description>Oracle: NCLOB</description></item>
    /// </list>
    /// This type is used for large text data requiring Unicode support.
    /// </summary>
    Text,

    /// <summary>
    /// A variable-length stream of binary data.
    /// <list type="bullet">
    /// <item><description>SQL Server: Binary</description></item>
    /// <item><description>PostgreSQL: Bytea</description></item>
    /// <item><description>MySQL: Blob</description></item>
    /// <item><description>SQLite: BLOB</description></item>
    /// <item><description>Oracle: BLOB</description></item>
    /// </list>
    /// Ranging between 1 and 8,000 bytes.
    /// </summary>
    VarBinary,

    /// <summary>
    /// A variable-length stream of binary data.
    /// <list type="bullet">
    /// <item><description>SQL Server: VarBinary</description></item>
    /// <item><description>PostgreSQL: Bytea</description></item>
    /// <item><description>MySQL: Blob</description></item>
    /// <item><description>SQLite: BLOB</description></item>
    /// <item><description>Oracle: BLOB</description></item>
    /// </list>
    /// Ranging between 1 and 8,000 bytes.
    /// </summary>
    Binary,

    /// <summary>
    /// A large Binary Large Object (BLOB) data type.
    /// <list type="bullet">
    /// <item><description>SQL Server: Image</description></item>
    /// <item><description>PostgreSQL: LargeObject</description></item>
    /// <item><description>MySQL: LongBlob</description></item>
    /// <item><description>SQLite: BLOB</description></item>
    /// <item><description>Oracle: BLOB</description></item>
    /// </list>
    /// Typically used for storing large files such as images, videos, or other multimedia content.
    /// </summary>
    Blob
}
