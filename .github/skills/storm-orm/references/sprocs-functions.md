# Stored procedures and functions

## Specification

### Stored procedures on StormContext

- Declare a private static method on the context with [StormProcedure].
- Method name drives the generated ExecuteX name (Execute + method name).
- Parameters use [StormParameter] with DbType, name, size, and direction.
- out parameters map to Output; ref parameters map to InputOutput.
- The method return type maps to ReturnValue.
- Generated method returns IExecuteProc<TResult> with ExecuteAsync.
- TResult derives from StormProcedureResult and exposes ReturnValue, RowsAffected, Exception, and output parameters.

### Stored procedures as db objects (result sets)

- Use [StormDbObject(ObjectType = DbObjectType.StoredProcedure)] on a model.
- Generated method returns IExecuteFrom<T, TOutput> with GetAsync, ListAsync, and StreamAsync.
- Use WithTracking or WithNoTracking if change tracking is needed.

### Scalar functions

- Declare a private static method on the context with [StormFunction].
- Generated method returns IExecuteScalarFunc<TResult> with GetAsync.

### Cancellation tokens

- Pass cancellation tokens to ExecuteAsync, GetAsync, ListAsync, and StreamAsync.
- Example: .ExecuteAsync(cancellationToken)

### Finding generated signatures

- If method names do not match, inspect Tests/TestModels/Generated/** to confirm generated method signatures.

## Examples

### Stored procedure with input/output/return value

Context declaration:

```csharp
[StormProcedure(ObjectName = "InputOutputProc", SchemaName = "test")]
private static int InputOutputProc(
	[StormParameter(ParameterName = "@user_id", DbType = UnifiedDbType.Int32)]
	DomainTypes.UserId? userId,
	[StormParameter(ParameterName = "@result_id", DbType = UnifiedDbType.Int32)]
	out int resultValue,
	[StormParameter(ParameterName = "@io", DbType = UnifiedDbType.Int32)]
	ref int io
) => throw new NotImplementedException();
```

Usage:

```csharp
var result = await context
	.ExecuteInputOutputProc(userId, 0)
	.ExecuteAsync(cancellationToken)
	.ConfigureAwait(false);

var returnValue = result.ReturnValue;
var outputValue = result.ResultValue;
var inputOutputValue = result.Io;
```

### Stored procedure returning rows

Model declaration:

```csharp
[StormDbObject<TestStormContext>(
	SchemaName = "dbo",
	ObjectName = "MyProc",
	ObjectType = DbObjectType.StoredProcedure)]
internal sealed partial record ProcUser
{
	[StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
	public DomainTypes.UserId UserId { get; set; }

	[StormColumn(ColumnType = ColumnType.PrimaryKey, DbType = UnifiedDbType.Int16)]
	public short BranchId { get; set; }
}
```

Usage:

```csharp
var users = await context
	.ExecuteMyProc()
	.WithNoTracking()
	.ListAsync(cancellationToken)
	.ConfigureAwait(false);
```

### Scalar function

Context declaration:

```csharp
[StormFunction(ObjectName = "ScalarFunc2", DbType = UnifiedDbType.Int32)]
private static CustomerId ScalarFunc(
	[StormParameter(ParameterName = "@user_id", DbType = UnifiedDbType.Int32)]
	DomainTypes.UserId userId,
	[StormParameter(ParameterName = "@branch_id", DbType = UnifiedDbType.Int16)]
	int branchId
) => default;
```

Usage:

```csharp
var customerId = await context
	.ExecuteScalarFunc(userId, branchId)
	.GetAsync(cancellationToken)
	.ConfigureAwait(false);
```
