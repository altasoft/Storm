# Transactions and scopes

## Specification

### Transaction scopes

- StormTransactionScope defines a transaction scope for StormContext operations.
- IsRoot is true only for the scope that created the transaction.
- Nested scopes can join an existing transaction or create a new one.

### Scope options

- JoinExisting (default): joins an existing scope or creates a new transaction if none exists.
- CreateNew: always creates a new transaction scope.

### Completion and rollback

- Call CompleteAsync to mark a scope successful; only the root scope commits.
- Disposing a scope without CompleteAsync rolls back the transaction.
- Nested JoinExisting scopes participate in the same transaction.
- CompleteAsync is idempotent; repeated calls are ignored.
- Dispose can be called multiple times safely.
- If an inner JoinExisting scope is disposed without CompleteAsync, the transaction is rolled back and outer CompleteAsync throws.

### External transactions

- A scope can wrap an existing transaction; it will not dispose the external connection/transaction on CompleteAsync.
- Disposing without CompleteAsync rolls back the external transaction.

### Cancellation tokens

- Pass cancellation tokens to CompleteAsync and any Storm async operations in the scope.

## Examples

### Basic transaction scope

```csharp
using var scope = new StormTransactionScope();
await using var context = new MyAppContext();

await context
	.UpdateUsersTable()
	.WithoutConcurrencyCheck()
	.Set(user1)
	.GoAsync(cancellationToken)
	.ConfigureAwait(false);

await context
	.UpdateUsersTable()
	.WithoutConcurrencyCheck()
	.Set(user2)
	.GoAsync(cancellationToken)
	.ConfigureAwait(false);

await scope.CompleteAsync(cancellationToken);
```

### Nested JoinExisting scopes

```csharp
using var outer = new StormTransactionScope();

using (var inner = new StormTransactionScope())
{
	// inner joins outer transaction
	await inner.CompleteAsync(cancellationToken);
}

await outer.CompleteAsync(cancellationToken);
```

### CreateNew scope

```csharp
using var outer = new StormTransactionScope();

using (var inner = new StormTransactionScope(StormTransactionScopeOption.CreateNew))
{
	// inner uses a new transaction scope
	await inner.CompleteAsync(cancellationToken);
}

await outer.CompleteAsync(cancellationToken);
```

### Mixed nesting: JoinExisting and CreateNew

```csharp
using var outer = new StormTransactionScope();

using var joinNested = new StormTransactionScope();

using (var newNested = new StormTransactionScope(StormTransactionScopeOption.CreateNew))
{
	await newNested.CompleteAsync(cancellationToken);
}

await joinNested.CompleteAsync(cancellationToken);
await outer.CompleteAsync(cancellationToken);
```

### External transaction

```csharp
await using var connection = new SqlConnection(connectionString);
await connection.OpenAsync(cancellationToken);
await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

using (var scope = new StormTransactionScope(transaction))
{
	await using var context = new MyAppContext(connectionString);

	await context
		.UpdateUsersTable()
		.WithoutConcurrencyCheck()
		.Set(user)
		.GoAsync(cancellationToken)
		.ConfigureAwait(false);

	await scope.CompleteAsync(cancellationToken);
}

await transaction.CommitAsync(cancellationToken);
```

### Inner JoinExisting disposed without CompleteAsync

```csharp
using var outer = new StormTransactionScope();

using (var innerJoin = new StormTransactionScope())
{
	// no CompleteAsync -> rollback on dispose
}

await Assert.ThrowsAsync<StormException>(() => outer.CompleteAsync(cancellationToken));
```

### CompleteAsync and Dispose are idempotent

```csharp
using var scope = new StormTransactionScope();

await scope.CompleteAsync(cancellationToken);
await scope.CompleteAsync(cancellationToken);

scope.Dispose();
scope.Dispose();
```

### Dispose without CompleteAsync rolls back

```csharp
using (var scope = new StormTransactionScope())
{
	await using var context = new MyAppContext(connectionString);

	await context
		.UpdateUsersTable()
		.WithoutConcurrencyCheck()
		.Set(user)
		.GoAsync(cancellationToken)
		.ConfigureAwait(false);

	// no CompleteAsync -> rollback on dispose
}
```

### Standalone context with batch inside scope

```csharp
await using var usersContext = new MyAppContext(connectionString);

var dbUsers = usersContext
	.SelectFromUsersTable()
	.Partially(User.PartialLoadFlags.Basic)
	.Where(x => x.UserId > 0)
	.OrderBy(User.OrderBy.UserId)
	.Top(3)
	.StreamAsync(cancellationToken);

using var scope = new StormTransactionScope();

await using var batchContext = new MyAppContext(connectionString);
await using var batch = batchContext.CreateBatch();

await foreach (var dbUser in dbUsers)
{
	dbUser.FullName = $"Updated_{dbUser.UserId}";
	var updateCmd = batchContext.UpdateUsersTable().WithoutConcurrencyCheck().Set(dbUser);
	batch.Add(updateCmd);
}

await batch.ExecuteAsync(cancellationToken);
await scope.CompleteAsync(cancellationToken);
```

