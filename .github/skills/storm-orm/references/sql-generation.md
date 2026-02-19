# SQL script generation

## Specification

### Generating SQL from C# models

Storm ORM does not automatically generate SQL DDL scripts from C# models. You manually write SQL CREATE TABLE and CREATE VIEW statements based on your [StormDbObject] and [StormColumn] attributes.

### Table generation

When you define a Storm C# model representing a table, you create the corresponding SQL table by mapping:

- [StormColumn(ColumnType = ColumnType.PrimaryKey)] → PRIMARY KEY constraint
- [StormColumn(ColumnType = ColumnType.PrimaryKeyAutoIncrement)] → PRIMARY KEY IDENTITY(1, 1)
- [StormColumn(ColumnType = ColumnType.Required)] → NOT NULL
- [StormColumn(DbType = UnifiedDbType.*)] → SQL column type (INT, VARCHAR, DECIMAL, DATETIME, etc.)
- [StormColumn(Size = X)] → VARCHAR(X), CHAR(X) length, or DECIMAL precision
- [StormColumn(Scale = X)] → DECIMAL scale digits
- [StormColumn(ColumnType = ColumnType.RowVersion)] → ROWVERSION

### View generation

When you define a Storm C# model for a database view, you create the SQL view by:

1. Writing the SELECT query that joins and filters from underlying tables
2. Creating the view with the same ObjectName from [StormDbObject]
3. Ensuring column names and types match the [StormColumn] definitions

## Examples

### Table from C# model

Given this Storm model:

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "Customers")]
public partial record Customer
{
    [StormColumn(ColumnType = ColumnType.PrimaryKeyAutoIncrement, ColumnName = "CustomerId")]
    public int Id { get; set; }

    [StormColumn(ColumnType = ColumnType.Required, DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string CompanyName { get; set; } = null!;

    [StormColumn(ColumnType = ColumnType.Required, DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string ContactName { get; set; } = null!;

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? Email { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 20)]
    public string? Phone { get; set; }

    [StormColumn(ColumnType = ColumnType.Required, DbType = UnifiedDbType.DateTime)]
    public DateTime CreatedAt { get; set; }

    [StormColumn(ColumnType = ColumnType.RowVersion | ColumnType.ConcurrencyCheck)]
    public SqlRowVersion RowVersion { get; set; }
}
```

Create the SQL table:

```sql
CREATE TABLE [dbo].[Customers] (
    [CustomerId] INT PRIMARY KEY IDENTITY(1, 1) NOT NULL,
    [CompanyName] VARCHAR(100) NOT NULL,
    [ContactName] VARCHAR(50) NOT NULL,
    [Email] VARCHAR(100) NULL,
    [Phone] VARCHAR(20) NULL,
    [CreatedAt] DATETIME NOT NULL,
    [RowVersion] ROWVERSION NOT NULL
);
```

### Table with composite primary key

Given this Storm model:

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "OrderDetails")]
public partial record OrderDetail
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey, DbType = UnifiedDbType.Int32, ColumnName = "OrderId")]
    public int OrderId { get; set; }

    [StormColumn(ColumnType = ColumnType.PrimaryKey, DbType = UnifiedDbType.Int32, ColumnName = "ProductId")]
    public int ProductId { get; set; }

    [StormColumn(ColumnType = ColumnType.Required, DbType = UnifiedDbType.Int32)]
    public int Quantity { get; set; }

    [StormColumn(ColumnType = ColumnType.Required, DbType = UnifiedDbType.Decimal, Size = 10, Scale = 2)]
    public decimal UnitPrice { get; set; }
}
```

Create the SQL table:

```sql
CREATE TABLE [dbo].[OrderDetails] (
    [OrderId] INT NOT NULL,
    [ProductId] INT NOT NULL,
    [Quantity] INT NOT NULL,
    [UnitPrice] DECIMAL(10, 2) NOT NULL,
    PRIMARY KEY ([OrderId], [ProductId])
);
```

### View from C# model

Given this Storm model representing a database view:

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "CustomerOrderSummary", ObjectType = DbObjectType.View)]
public partial record CustomerOrderSummary
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "CustomerId")]
    public int CustomerId { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? CompanyName { get; set; }

    [StormColumn(DbType = UnifiedDbType.Int32)]
    public int? TotalOrders { get; set; }

    [StormColumn(DbType = UnifiedDbType.Decimal, Size = 15, Scale = 2)]
    public decimal? TotalAmount { get; set; }

    [StormColumn(DbType = UnifiedDbType.DateTime)]
    public DateTime? LastOrderDate { get; set; }
}
```

Create the SQL view:

```sql
CREATE VIEW [dbo].[CustomerOrderSummary]
AS
SELECT 
    c.CustomerId,
    c.CompanyName,
    COUNT(o.OrderId) AS TotalOrders,
    SUM(od.UnitPrice * od.Quantity) AS TotalAmount,
    MAX(o.OrderDate) AS LastOrderDate
FROM [dbo].[Customers] c
LEFT JOIN [dbo].[Orders] o ON c.CustomerId = o.CustomerId
LEFT JOIN [dbo].[OrderDetails] od ON o.OrderId = od.OrderId
GROUP BY c.CustomerId, c.CompanyName;
```

### View joining multiple tables

Given this Storm model:

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "EmployeeSalesView", ObjectType = DbObjectType.View)]
public partial record EmployeeSalesInfo
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "EmployeeId")]
    public int EmployeeId { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string? FirstName { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string? LastName { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? DepartmentName { get; set; }

    [StormColumn(DbType = UnifiedDbType.Decimal, Size = 15, Scale = 2)]
    public decimal? TotalSales { get; set; }
}
```

Create the SQL view:

```sql
CREATE VIEW [dbo].[EmployeeSalesView]
AS
SELECT 
    e.EmployeeId,
    e.FirstName,
    e.LastName,
    d.DepartmentName,
    SUM(od.UnitPrice * od.Quantity) AS TotalSales
FROM [dbo].[Employees] e
INNER JOIN [dbo].[Departments] d ON e.DepartmentId = d.DepartmentId
LEFT JOIN [dbo].[Orders] o ON e.EmployeeId = o.EmployeeId
LEFT JOIN [dbo].[OrderDetails] od ON o.OrderId = od.OrderId
GROUP BY e.EmployeeId, e.FirstName, e.LastName, d.DepartmentName;
```

### Mapping reference

When translating C# Storm attributes to SQL syntax:

| Storm Attribute | SQL Translation |
|---|---|
| `ColumnType.PrimaryKey` | PRIMARY KEY |
| `ColumnType.PrimaryKeyAutoIncrement` | PRIMARY KEY IDENTITY(1, 1) |
| `ColumnType.Required` | NOT NULL |
| `DbType.AnsiString, Size=100` | VARCHAR(100) |
| `DbType.Int32` | INT |
| `DbType.DateTime` | DATETIME |
| `DbType.Decimal, Size=10, Scale=2` | DECIMAL(10, 2) |
| `DbType.Bit` | BIT |
| `DbType.BigInt` | BIGINT |
| `ColumnType.RowVersion` | ROWVERSION |
| `ObjectType = DbObjectType.View` | CREATE VIEW |

1. **Keep DDL and attributes in sync** - If you change column size in SQL, update [StormColumn] Size
2. **Use consistent naming** - Match C# property names to database column names, or use ColumnName attribute
3. **Define constraints in database** - Primary keys, unique constraints, foreign keys are defined in DDL
4. **Version your schema changes** - Use a migration tool or change log to track schema evolution
5. **Test column type compatibility** - Ensure UnifiedDbType mapping matches actual SQL column type and size
