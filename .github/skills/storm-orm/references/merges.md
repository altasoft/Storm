# Merges

## Specification

### Generated merge entry points

- MergeIntoX is generated for tables with ObjectType = DbObjectType.Table and for CustomSqlStatement.
- Method name pattern: MergeInto + DisplayName (if specified) or ObjectName.
- MergeIntoX() returns AltaSoft.Storm.Crud.IMergeInto<T>.
- For CustomSqlStatement, overloads accept customQuotedObjectFullName:
  - MergeIntoX(customQuotedObjectFullName)

### Merge actions

- UpdateOrInsert updates first; inserts if not found.
- InsertOrUpdate inserts first; updates if already exists.
- Both actions accept a single entity or IEnumerable<T>.
- Use WithConcurrencyCheck or WithoutConcurrencyCheck to control concurrency behavior.

### Terminal method

- GoAsync executes the merge.

### Cancellation tokens

- Cancellation tokens are supported by async methods; pass them to GoAsync.
- Example: .GoAsync(cancellationToken)

## Examples

### Merge (upsert) - UpdateOrInsert

```csharp
await using var context = new MyAppContext();

var user = new User
{
    UserId = 100,
    Name = "John Doe",
    Status = UserStatus.Active
};

await context
    .MergeIntoUsersTable()
    .UpdateOrInsert(user)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Merge (upsert) - InsertOrUpdate

```csharp
await using var context = new MyAppContext();

var user = new User
{
    UserId = 100,
    Name = "John Doe",
    Status = UserStatus.Active
};

await context
    .MergeIntoUsersTable()
    .InsertOrUpdate(user)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Batch merge

```csharp
await using var context = new MyAppContext();

var users = new[]
{
    new User { UserId = 100, Name = "User 100" },
    new User { UserId = 101, Name = "User 101" },
    new User { UserId = 102, Name = "User 102" }
};

await context
    .MergeIntoUsersTable()
    .UpdateOrInsert(users)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Merge with concurrency control

```csharp
await context
    .MergeIntoUsersTable()
    .WithConcurrencyCheck()
    .UpdateOrInsert(user)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);

await context
    .MergeIntoUsersTable()
    .WithoutConcurrencyCheck()
    .InsertOrUpdate(user)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### CustomSqlStatement merge

```csharp
const string tableName = "dbo.BlobTest1";

await context
    .MergeIntoBlob(tableName)
    .UpdateOrInsert(new Blob
    {
        Metadata = "",
        BigString = "2321311",
        Id = 1,
        SomeOtherValue = 1
    })
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```
