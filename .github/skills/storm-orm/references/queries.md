# Queries and projections

## Contents
- [Specification](#specification)
  - [Generated query entry points](#generated-query-entry-points)
  - [Unique indexes for fast lookups](#unique-indexes-for-fast-lookups)
  - [Cancellation tokens](#cancellation-tokens)
  - [Single-record retrieval](#single-record-retrieval)
  - [List retrieval](#list-retrieval)
  - [Count and existence](#count-and-existence)
  - [Where clause definitions](#where-clause-definitions)
  - [Ordering and paging](#ordering-and-paging)
  - [Projections](#projections)
  - [Partial load](#partial-load)
  - [Tracking](#tracking)
  - [Streaming](#streaming)
- [Examples](#examples)
  - [Basic selects](#basic-selects)
  - [Unique index lookups](#unique-index-lookups)
  - [Count and exists](#count-and-exists)
  - [Where clause patterns](#where-clause-patterns)
  - [OrderBy](#orderby)
  - [Top and Skip (pagination)](#top-and-skip-pagination)
  - [Projections](#projections-1)
  - [Partial load](#partial-load-1)
  - [Change tracking](#change-tracking)
  - [Table hints](#table-hints)
  - [Streaming large result sets](#streaming-large-result-sets)
  - [Detail table loading](#detail-table-loading)

## Specification

### Generated query entry points

- Selectors are generated for tables, views, virtual views, and table-valued functions.
- Method name pattern: SelectFrom + DisplayName (if specified) or ObjectName.
- For tables with primary keys, an overload is generated that accepts the primary key parameters.
- For tables with unique indexes marked with [StormIndex(IsUnique = true)], additional overloads are generated that accept those index column parameters.
- Builder methods are chainable; execution happens only on the terminal method call.
- Terminal methods include GetAsync, ListAsync, CountAsync, ExistsAsync, and StreamAsync.

### Unique indexes for fast lookups

- [StormIndex] is declared at the class level (not on properties).
- Specify property names as a string array and IsUnique flag: [StormIndex(["Email"], true)].
- For composite indexes, list multiple property names in the array: [StormIndex(["WarehouseId", "Location"], true)].
- The generated method signature becomes SelectFromX(indexColumnParam...).GetAsync() for single-record retrieval.
- Example: If Email is marked as a unique index, SelectFromUsersTable(email) is generated to fetch by email directly.
- Unique index lookups are useful for alternate keys (email, username, external ID, etc.) without using a Where clause.

### Cancellation tokens

- Cancellation tokens are supported by async methods; pass them to GetAsync, ListAsync, CountAsync, ExistsAsync, and StreamAsync.
- Example: .ListAsync(cancellationToken)

### Single-record retrieval

- Single by primary key: SelectFromX(primaryKey...).GetAsync()
- Single by filter: SelectFromX().Where(...).GetAsync()
- Single without filter: SelectFromX().GetAsync()
- Behavior: returns the first row or null if no row is found.

### List retrieval

- List all or filtered rows: SelectFromX().Where(...).ListAsync()
- Behavior: returns an empty list if no rows are found.

### Count and existence

- CountAsync returns the count for the current selector.
- ExistsAsync returns true if at least one row matches the current selector.

### Where clause definitions

- Expression-based: Where(x => ...)
- OData string: Where("...")
- In / Contains: Use x.Column.In(values) to generate SQL IN (...)
- In with an empty collection returns no rows.
- Null checks: Use x.Column == null or x.Column != null

### Ordering and paging

- OrderBy enum is generated per model with ascending/descending variants of the PropertyNames.
- OrderByKey orders by primary key columns.
- Use Skip and Top for pagination; apply OrderBy before paging.

### Projections

- GetAsync(x => x.Prop) returns a result with RowFound and Value.
- GetAsync(x => x.Prop1, x => x.Prop2) returns a tuple in Value.

### Partial load

- PartialLoadFlags are generated per model and control properties and detail tables.

### Tracking

- WithNoTracking is the default for read-only queries.
- WithTracking enables change detection for updates.

### Streaming

- StreamAsync returns IAsyncEnumerable<T> for large result sets.
- StreamAsync cannot load detail tables.

## Examples

### Basic selects

#### Get single record by primary key

```csharp
await using var context = new MyAppContext();

var user = await context
    .SelectFromUsersTable(5)
    .GetAsync(cancellationToken)
    .ConfigureAwait(false);

// Returns null if not found
if (user is null)
{
    // Handle not found
}
```

#### Get list with conditions

```csharp
await using var context = new MyAppContext();

var users = await context
    .SelectFromUsersTable()
    .Where(x => x.Id > 10)
    .OrderBy(User.OrderByKey)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Unique index lookups

When a column or set of columns is marked with [StormIndex(IsUnique = true)], Storm generates direct lookup overloads.

#### Model with unique index

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "Users")]
[StormIndex(["Email"], true)]
public partial record User
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public int UserId { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? Email { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string? Username { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? FullName { get; set; }
}
```

#### Using unique index overload to fetch by email

```csharp
await using var context = new MyAppContext();

var user = await context
    .SelectFromUsersTable("john@example.com")  // Direct lookup by unique email
    .GetAsync(cancellationToken)
    .ConfigureAwait(false);
```

#### Composite unique index

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "InventoryItems")]
[StormIndex(["WarehouseId", "Location"], true)]
public partial record InventoryItem
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public int ItemId { get; set; }

    [StormColumn(DbType = UnifiedDbType.Int32)]
    public int WarehouseId { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string Location { get; set; } = null!;

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? Description { get; set; }
}
```

Using composite unique index:

```csharp
await using var context = new MyAppContext();

var item = await context
    .SelectFromInventoryItems(warehouseId: 5, location: "A-12-B")
    .GetAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Count and exists

```csharp
await using var context = new MyAppContext();

var count = await context
    .SelectFromUsersTable()
    .CountAsync(cancellationToken)
    .ConfigureAwait(false);

var exists = await context
    .SelectFromUsersTable()
    .Where(x => x.Id == 10)
    .ExistsAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Where clause patterns

#### Simple conditions

```csharp
await context
    .SelectFromUsersTable()
    .Where(x => x.Status == UserStatus.Active && x.Balance > 100)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);
```

#### In/Contains

```csharp
var ids = new[] { 1, 2, 3, 4 };
await context
    .SelectFromUsersTable()
    .Where(x => x.Id.In(ids))
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);
```

#### Null checks

```csharp
await context
    .SelectFromUsersTable()
    .Where(x => x.Notes != null)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);
```

#### OData filter (brief)

```csharp
await context
    .SelectFromUsersTable()
    .Where("Status eq 'Active' and Balance gt 100")
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);
```

### OrderBy

OrderBy enum is auto-generated per model with all columns and ascending/descending variants.
OrderByKey orders by primary key columns.

```csharp
await context
    .SelectFromUsersTable()
    .OrderBy(User.OrderBy.Name_Asc)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);

await context
    .SelectFromUsersTable()
    .OrderBy(User.OrderBy.CreatedAt_Desc)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);

await context
    .SelectFromUsersTable()
    .OrderBy(User.OrderByKey)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Top and Skip (pagination)

```csharp
await context
    .SelectFromUsersTable()
    .OrderBy(User.OrderByKey)
    .Top(10)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);

await context
    .SelectFromUsersTable()
    .OrderBy(User.OrderByKey)
    .Skip(20)
    .Top(10)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Projections

#### Single property

```csharp
var result = await context
    .SelectFromUsersTable(5)
    .GetAsync(x => x.Name, cancellationToken)
    .ConfigureAwait(false);

// result.RowFound is true if record exists
// result.Value contains the Name value
if (result.RowFound)
{
    //record exists
}
```

#### Multiple properties (tuple)

```csharp
var result = await context
    .SelectFromUsersTable(5)
    .GetAsync(x => x.Name, x => x.Balance, cancellationToken)
    .ConfigureAwait(false);

if (result.RowFound)
{
    var (name, balance) = result.Value;
}
```

### Partial load

PartialLoadFlags are auto-generated per model and control which properties and detail tables are loaded.

```csharp
await using var context = new MyAppContext();

var users = await context
    .SelectFromUsersTable()
    .Partially(User.PartialLoadFlags.Basic)
    .OrderBy(User.OrderByKey)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);

// Load specific flags
var usersWithProfile = await context
    .SelectFromUsersTable()
    .Partially(User.PartialLoadFlags.Profile | User.PartialLoadFlags.Settings)
    .OrderBy(User.OrderByKey)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);
```

Partial load is useful for:
- Reducing data transfer when only specific fields are needed.
- Avoiding loading detail tables or large JSON/XML columns.
- Improving query performance.

### Change tracking

Use WithTracking when you need to detect property changes for updates.
Use WithNoTracking (default) for read-only queries.

```csharp
var user = await context
    .SelectFromUsersTable(5)
    .WithTracking()
    .GetAsync(cancellationToken)
    .ConfigureAwait(false);

user.Name = "Updated Name";
await context
    .UpdateUsersTable()
    .Set(user)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Table hints

Use WithTableHints for query optimization (NOLOCK, etc).

```csharp
var users = await context
    .SelectFromUsersTable()
    .WithTableHints(StormTableHints.NoLock)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Streaming large result sets

Use StreamAsync to process large result sets without loading all records into memory.
Returns IAsyncEnumerable<T>.

```csharp
await using var context = new MyAppContext();

var users = context
    .SelectFromUsersTable()
    .Where(x => x.Status == UserStatus.Active)
    .OrderBy(User.OrderByKey)
    .Top(1000)
    .StreamAsync(cancellationToken);

await foreach (var user in users)
{
    // Process each user
    await ProcessUserAsync(user);
}
```

Note: StreamAsync cannot load detail tables. Use ListAsync if detail tables are needed.

### Detail table loading

Properties marked with SaveAs = SaveAs.DetailTable generate PartialLoadFlags for controlling whether detail data loads.

Model example:

```csharp
[StormDbObject<MyAppContext>(ObjectType = DbObjectType.Table)]
public sealed partial class User
{
    public int UserId { get; set; }
    public string Name { get; set; }
    
    // Detail table with explicit name
    [StormColumn(SaveAs = SaveAs.DetailTable, DetailTableName = "UserAddresses")]
    public List<Address>? Addresses { get; set; }
    
    // Detail table with inferred name (Cars)
    [StormColumn(SaveAs = SaveAs.DetailTable)]
    public EntityTrackingList<Car>? Cars { get; set; }
    
    // Simple array (not a detail table)
    public List<int>? RoleIds { get; set; }
}
```

Loading with detail tables:

```csharp
await using var context = new MyAppContext();

// Load without detail tables (default)
var users = await context
    .SelectFromUsersTable()
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);

// Load with specific detail tables
var usersWithCars = await context
    .SelectFromUsersTable()
    .Partially(User.PartialLoadFlags.Cars)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);

// Load with multiple detail tables
var usersWithDetails = await context
    .SelectFromUsersTable()
    .Partially(User.PartialLoadFlags.Addresses | User.PartialLoadFlags.Cars)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);
```
