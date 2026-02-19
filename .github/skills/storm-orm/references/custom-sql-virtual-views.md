# Custom SQL and virtual views

## Specification

### Virtual views

- Use [StormDbObject] with ObjectType = DbObjectType.VirtualView and VirtualViewSql.
- DisplayName controls the generated method name: SelectFrom + DisplayName.
- VirtualViewSql can use the {%schema%} token.
- Query with SelectFromXVirtualView() and normal selector APIs.

### Custom SQL statements

- Use [StormDbObject] with ObjectType = DbObjectType.CustomSqlStatement.
- Query with SelectFromXCustomSql(customSqlStatement, callParameters).
- callParameters is optional and uses StormCallParameter.

### Cancellation tokens

- Pass cancellation tokens to GetAsync, ListAsync, and StreamAsync.


## Examples

### Virtual view declaration

```csharp
[StormDbObject<TestStormContext>(
	DisplayName = "UsersVirtualView",
	ObjectType = DbObjectType.VirtualView,
	VirtualViewSql = """
		SELECT * FROM {%schema%}.Users
		WHERE Id > 5
		""")]
public partial record User
{
	[StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
	public DomainTypes.UserId UserId { get; set; }

	[StormColumn(ColumnType = ColumnType.PrimaryKey, DbType = UnifiedDbType.Int16)]
	public short BranchId { get; set; }
}
```

### Virtual view query

```csharp
var users = await context
	.SelectFromUsersVirtualView()
	.OrderBy(User.OrderByKey)
	.ListAsync(cancellationToken)
	.ConfigureAwait(false);

var single = await context
	.SelectFromUsersVirtualView(userId, branchId)
	.GetAsync(cancellationToken)
	.ConfigureAwait(false);
```

### Custom SQL declaration

```csharp
[StormDbObject<TestStormContext>(DisplayName = "UsersCustomSql", ObjectType = DbObjectType.CustomSqlStatement)]
public partial record User
{
	[StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
	public DomainTypes.UserId UserId { get; set; }

	[StormColumn(ColumnType = ColumnType.PrimaryKey, DbType = UnifiedDbType.Int16)]
	public short BranchId { get; set; }
}
```

### Custom SQL query with parameters

```csharp
const string customSql = "SELECT * FROM dbo.Users WHERE Id > @min_user_id";

var callParams = new List<StormCallParameter>(1)
{
	new("@min_user_id", UnifiedDbType.Int32, 5)
};

var users = await context
	.SelectFromUsersCustomSql(customSql, callParams)
	.OrderBy(User.OrderByKey)
	.ListAsync(cancellationToken)
	.ConfigureAwait(false);

var single = await context
	.SelectFromUsersCustomSql(userId, branchId, customSql, callParams)
	.GetAsync(cancellationToken)
	.ConfigureAwait(false);
```
