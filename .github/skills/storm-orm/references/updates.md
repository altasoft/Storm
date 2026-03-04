# Updates and patches

## Specification

### Generated update entry points

- UpdateX is generated for tables with ObjectType = DbObjectType.Table and for CustomSqlStatement.
- Method name pattern: Update + DisplayName (if specified) or ObjectName.
- UpdateX() returns AltaSoft.Storm.Crud.IUpdateFrom<T>.
- UpdateX(primaryKey...) returns AltaSoft.Storm.Crud.IUpdateFromSingle<T>.
- For CustomSqlStatement, overloads accept customQuotedObjectFullName:
  - UpdateX(customQuotedObjectFullName)
  - UpdateX(primaryKey..., customQuotedObjectFullName)

### Builder interfaces

- IUpdateFrom<T> supports Set(entity), Set(entities), and Set(column, value).
- IUpdateFromSet<T> supports Where(...), Top(...), and additional Set calls.
- IUpdateFromSingle<T> supports Set(entity) and Set(column, value).
- IUpdateFromSetSingle<T> supports additional Set calls for single-row updates.
- Builder methods are chainable; execution happens only on the terminal method call.

### Terminal method

- GoAsync executes the update.

### Cancellation tokens

- Cancellation tokens are supported by async methods; pass them to GoAsync.
- Example: .GoAsync(cancellationToken)

### Concurrency control

- Use WithConcurrencyCheck or WithoutConcurrencyCheck explicitly for updates.
- Properties marked with ColumnType = ColumnType.ConcurrencyCheck or ColumnType.RowVersion are checked by default.

## Examples

### Update by entity

```csharp
await using var context = new MyAppContext();

var user = await context
    .SelectFromUsersTable(5)
    .WithTracking()
    .GetAsync(cancellationToken)
    .ConfigureAwait(false);

user.Name = "Updated Name";
user.Balance = 1000;

await context
    .UpdateUsersTable()
    .Set(user)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Update by primary key

```csharp
await using var context = new MyAppContext();

await context
    .UpdateUsersTable(10)
    .Set(x => x.Status, UserStatus.NotActive)
    .Set(x => x.Balance, 0)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Update by predicate

```csharp
await using var context = new MyAppContext();

await context
    .UpdateUsersTable()
    .Set(x => x.Status, UserStatus.NotActive)
    .Where(x => x.Balance < 100)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Update with OData filter

```csharp
await context
    .UpdateUsersTable()
    .Set(x => x.Status, UserStatus.NotActive)
    .Where("Status eq 'Active'")
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Update with Top

```csharp
await context
    .UpdateUsersTable()
    .Set(x => x.Status, UserStatus.NotActive)
    .Top(10)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Update with value selector

```csharp
await context
    .UpdateUsersTable()
    .Set(x => x.UpdatedAt, x => x.CreatedAt)
    .Where(x => x.UpdatedAt == null)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Batch updates

```csharp
await using var context = new MyAppContext();

var users = await context
    .SelectFromUsersTable()
    .WithTracking()
    .Where(x => x.Status == UserStatus.Active)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);

foreach (var user in users)
{
    user.Status = UserStatus.Inactive;
}

await context
    .UpdateUsersTable()
    .WithoutConcurrencyCheck()
    .Set(users)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Concurrency control

```csharp
// With concurrency check (default for properties marked ConcurrencyCheck)
await context
    .UpdateUsersTable()
    .Set(user)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);

// Explicitly without concurrency check
await context
    .UpdateUsersTable()
    .WithoutConcurrencyCheck()
    .Set(user)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### CustomSqlStatement update

```csharp
const string tableName = "dbo.BlobTest1";

await context
    .UpdateBlob(tableName)
    .Set(x => x.Metadata, "some metadata")
    .Where(x => x.Id == 1)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);

await context
    .UpdateBlob(1, tableName)
    .Set(x => x.Metadata, "new metadata")
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```
