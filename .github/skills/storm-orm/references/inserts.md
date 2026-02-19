# Inserts and bulk inserts

## Specification

### Generated insert entry points

- InsertIntoX is generated for tables with ObjectType = DbObjectType.Table.
- BulkInsertIntoX is generated only if BulkInsert = true on the StormDbObject attribute.
- Builder methods are chainable; execution happens only on the terminal method call.
- Terminal method: GoAsync.

### Cancellation tokens

- Cancellation tokens are supported by async methods; pass them to GoAsync.
- Example: .GoAsync(cancellationToken)

### Single and batch inserts

- Single insert: InsertIntoX().Values(entity).GoAsync()
- Batch insert: InsertIntoX().Values(entities).GoAsync()

### Auto-increment ID retrieval

- ColumnType.AutoIncrement and ColumnType.PrimaryKeyAutoIncrement vlaues are assigned after insert.
- The auto-increment property must have a setter for assignment.

### Bulk insert sources

- BulkInsertIntoX().Values(IEnumerable<T>)
- BulkInsertIntoX().Values(IAsyncEnumerable<T>)
- BulkInsertIntoX().Values(ChannelReader<T>)

## Examples

### Single insert

```csharp
await using var context = new MyAppContext();

var user = new User { Name = "John", ExternalId = "EXT1" };
await context
    .InsertIntoUsersTable()
    .Values(user)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Auto-increment ID retrieval

When a property is marked with ColumnType.AutoIncrement or ColumnType.PrimaryKeyAutoIncrement, the generated ID is automatically assigned to the property after insert.

Important: The auto-increment property must have a setter for the value to be assigned.

```csharp
await using var context = new MyAppContext();

var user = new User { Name = "John", ExternalId = "EXT1" };
await context
    .InsertIntoUsersTable()
    .Values(user)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);

```

### Batch insert

```csharp
await using var context = new MyAppContext();

var users = new[] { user1, user2, user3 };
await context
    .InsertIntoUsersTable()
    .Values(users)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);

// Each user now has its generated auto-increment ID assigned
```

### Bulk insert from IEnumerable

```csharp
await using var context = new MyAppContext();

var users = new List<User> { user1, user2, user3 };
await context
    .BulkInsertIntoUsersTable()
    .Values(users)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Bulk insert from IAsyncEnumerable

```csharp
await using var context = new MyAppContext();

await context
    .BulkInsertIntoUsersTable()
    .Values(GetUsersAsync())
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);

async IAsyncEnumerable<User> GetUsersAsync()
{
    for (int i = 0; i < 1000; i++)
        yield return new User { Name = $"User{i}" };
}
```

### Bulk insert from Channel

```csharp
await using var context = new MyAppContext();

var channel = Channel.CreateUnbounded<User>();

var producer = Task.Run(async () =>
{
    for (int i = 0; i < 1000; i++)
        await channel.Writer.WriteAsync(new User { Name = $"User{i}" });
    channel.Writer.Complete();
});

await context
    .BulkInsertIntoUsersTable()
    .Values(channel.Reader)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
await producer;
```
