# Model binding and attribute mapping

Use StormDbObject to map a C# type to a db object and to drive code generation.
StormDbObject is the source of the generated CRUD and execution methods on your StormContext.
You can apply multiple StormDbObject attributes to the same class to bind it to multiple objects.

Key ideas:

- The generic type argument is the StormContext that owns the generated methods.
- ObjectType controls which methods are generated.
- DisplayName controls the generated method suffix (for example, UsersTable -> SelectFromUsersTable).
- SchemaName and ObjectName define the db object name.

Typical method generation (names vary by DisplayName):

- Table: SelectFromX, InsertIntoX, UpdateX, DeleteFromX, MergeIntoX, BulkInsertIntoX (if bulk insert enabled).
    Key-based overloads like SelectFromX(id...) and UpdateX(id...) are generated only when a primary key is defined.
- View or virtual view: SelectFromX (read-only).
- Table-valued function: SelectFromX (function parameters become method parameters).
- Stored procedure: ExecuteX (input and output parameters map to method parameters).
- Scalar function: ExecuteScalarX.
- Custom SQL: SelectFromXCustomSql(customSql).

If method names are unclear, ask the user to provide the generated signatures or their naming conventions.

Use StormColumn only when required. If a property is a simple mapped column and no special rules apply, omit StormColumn.

StormColumn is required when:

1. ColumnType is not Default (key, identity, concurrency, row version, etc).
2. SaveAs is not Default.
3. LoadWithFlags is set or DetailTableName is set.
4. ColumnName differs from the C# property name.
5. DbType must be specified (for example, Ansi vs Unicode string or ambiguous types).
6. Size, Precision, or Scale must be specified.

**Well-known scalar types**: StormColumn is NOT required for well-known .NET types that have automatic mappings:
- `DateTime` (C#) automatically maps to `datetime2` (SQL Server) — no DbType attribute needed
- `DateTime?` (C#) for nullable `datetime2` (SQL Server) — no DbType attribute needed
- When SQL table has `datetime2` or `datetime`, simply use `DateTime` or `DateTime?` property without `[StormColumn]`

ColumnType values (flags):

- Default
- PrimaryKey
- AutoIncrement
- ConcurrencyCheck
- RowVersion
- HasDefaultValue
- ConditionalTerminator
- Immutable
- PrimaryKeyAutoIncrement (PrimaryKey | AutoIncrement)

SaveAs values:

- Default: Store well-known scalar types directly; ignore unknown complex types.
- String: Store as plain string (useful for enum-as-text).
- CompressedString: Store a string compressed as binary.
- Json: Serialize any type to JSON text and store as string.
- CompressedJson: Serialize any type to JSON, compress, and store as binary.
- Xml: Serialize any type to XML text and store as string.
- CompressedXml: Serialize any type to XML, compress, and store as binary.
- FlatObject: Flatten object properties into separate columns.
- DetailTable: Store a collection in a detail table (master-detail).
- Ignore: Do not persist this property.

Compression storage guidance:

- CompressedJson and CompressedXml use varbinary storage.
- Json and Xml use string storage. Prefer Unicode string by default; specify Ansi only when needed.

DbType guidance:

- Avoid specifying DbType unless needed.
- Use DbType for string kinds (Ansi vs Unicode), fixed-length vs variable, and for non-obvious mappings.
- Use Size, Precision, and Scale when the database type requires it (varchar/nvarchar, decimal, etc).
- **Do NOT use DbType for well-known scalar types**: `DateTime` (for SQL `datetime2`), `int` (for SQL `int`), `bool` (for SQL `bit`), `decimal` (for SQL `decimal`), `Guid` (for SQL `uniqueidentifier`) — these map automatically.

Sample table mapping pattern:

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "Users", DisplayName = "UsersTable", ObjectType = DbObjectType.Table)]
public sealed partial class User
{
    [StormColumn(ColumnType = ColumnType.PrimaryKeyAutoIncrement)]
    public int Id { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50, ColumnName = "external_id")]
    public string ExternalId { get; set; }

    [StormColumn(DbType = UnifiedDbType.String, Size = 200)]
    public string Name { get; set; }

    [StormColumn(SaveAs = SaveAs.String, Size = 20)]
    public UserStatus Status { get; set; }

    [StormColumn(DbType = UnifiedDbType.Decimal, Precision = 18, Scale = 2)]
    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; }

    [StormColumn(SaveAs = SaveAs.Json, DbType = UnifiedDbType.Json)]
    public UserProfile Profile { get; set; }

    [StormColumn(SaveAs = SaveAs.CompressedJson, DbType = UnifiedDbType.VarBinary)]
    public UserSettings Settings { get; set; }

    [StormColumn(ColumnType = ColumnType.RowVersion | ColumnType.ConcurrencyCheck)]
    public SqlRowVersion RowVersion { get; set; }

    public string? Notes { get; set; }
    
    public int Version{get;set;}
}
```

Table script mapping tips:

- PK and identity -> ColumnType.PrimaryKey or ColumnType.PrimaryKeyAutoIncrement.
- NOT NULL vs NULL -> non-nullable vs nullable types.
- varchar or nvarchar length -> UnifiedDbType + Size.
- **datetime2 -> use `DateTime` or `DateTime?` (C# type) without [StormColumn] attribute** (automatic well-known type mapping).
- varbinary(max) -> UnifiedDbType.VarBinary with Size = -1 (or omit size if allowed).

Ask for schema, PK, identity, and nullability if missing.
