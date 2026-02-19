# Deletes

## Specification

### Generated delete entry points

- DeleteFromX is generated for tables with ObjectType = DbObjectType.Table and for CustomSqlStatement.
- Method name pattern: DeleteFrom + DisplayName (if specified) or ObjectName.
- DeleteFromX() returns AltaSoft.Storm.Crud.IDeleteFrom<T>.
- DeleteFromX(primaryKey...) returns AltaSoft.Storm.Crud.IDeleteFromSingle<T>.
- DeleteFromX(entity) and DeleteFromX(IEnumerable<entity>) return IDeleteFromSingle<T>.
- For CustomSqlStatement, overloads accept customQuotedObjectFullName:
  - DeleteFromX(customQuotedObjectFullName)
  - DeleteFromX(primaryKey..., customQuotedObjectFullName)
  - DeleteFromX(entity, customQuotedObjectFullName)
  - DeleteFromX(IEnumerable<entity>, customQuotedObjectFullName)

### Delete builder interfaces

- IDeleteFrom<T> supports Where(...), Top(...), and builder configuration.
- IDeleteFromSingle<T> is used for key/entity deletes.
- Builder methods are chainable; execution happens only on the terminal method call.

### Terminal method

- GoAsync executes the delete.

### Cancellation tokens

- Cancellation tokens are supported by async methods; pass them to GoAsync.
- Example: .GoAsync(cancellationToken)

## Examples

### Delete by primary key

```csharp
await using var context = new MyAppContext();

await context
    .DeleteFromUsersTable(10)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Delete by entity

```csharp
await using var context = new MyAppContext();

var user = await context
    .SelectFromUsersTable(10)
    .GetAsync(cancellationToken)
    .ConfigureAwait(false);

await context
    .DeleteFromUsersTable(user)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Delete by predicate

```csharp
await using var context = new MyAppContext();

await context
    .DeleteFromUsersTable()
    .Where(x => x.Status == UserStatus.Inactive && x.Balance == 0)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Delete with OData filter

```csharp
await context
    .DeleteFromUsersTable()
    .Where("Status eq 'Inactive' and Balance eq 0")
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Delete with Top

```csharp
await context
    .DeleteFromUsersTable()
    .Where(x => x.Status == UserStatus.Inactive)
    .Top(10)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### Batch delete

```csharp
await using var context = new MyAppContext();

var inactiveUsers = await context
    .SelectFromUsersTable()
    .Where(x => x.Status == UserStatus.Inactive)
    .ListAsync(cancellationToken)
    .ConfigureAwait(false);

await context
    .DeleteFromUsersTable(inactiveUsers)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```

### CustomSqlStatement delete

```csharp
const string tableName = "dbo.BlobTest1";

await context
    .DeleteFromBlob(1, tableName)
    .GoAsync(cancellationToken)
    .ConfigureAwait(false);
```
