# Model generation from database

## Contents
- [Specification](#specification)
  - [Database-first approach](#database-first-approach)
  - [Attribute-based declaration](#attribute-based-declaration)
  - [Model structure](#model-structure)
  - [Model variants](#model-variants)
- [Examples](#examples)
  - [Table model](#table-model)
  - [View model](#view-model)
  - [Table-valued function model](#table-valued-function-model)
  - [Stored procedure returning rows](#stored-procedure-returning-rows)
  - [Virtual view model](#virtual-view-model)
  - [Custom SQL model](#custom-sql-model)
  - [Model with multiple variants](#model-with-multiple-variants)
  - [Procedure with parameters](#procedure-with-parameters)
  - [From SQL script to Storm model](#from-sql-script-to-storm-model)
  - [Scalar function](#scalar-function)
  - [Stored procedure with input parameters only](#stored-procedure-with-input-parameters-only)
  - [Stored procedure with input and output parameters](#stored-procedure-with-input-and-output-parameters)
  - [Stored procedure with return value](#stored-procedure-with-return-value)
  - [Table-valued function returning rows](#table-valued-function-returning-rows)
  - [View model with joins](#view-model-with-joins)

## Specification

### Database-first approach

- Storm ORM does not provide automatic DDL introspection or code generation from existing database objects.
- Models are created manually by defining classes with [StormDbObject] attributes.
- Column mappings are specified using [StormColumn] and [StormParameter] attributes.

### Attribute-based declaration

- [StormDbObject<TContext>] declares the db object type, schema, and object name.
- [StormColumn] declares how properties map to database columns.
- [StormParameter] (for procedures/functions) declares parameter name, direction, and type.
- Attributes are the source of truth; the model drives the generated CRUD API.

### Model structure

- Models are C# records or classes.
- Each model corresponds to a table, view, stored procedure, function, custom SQL, or virtual view.
- Primary keys are marked with ColumnType.PrimaryKey.
- Identity columns use ColumnType.AutoIncrement or ColumnType.PrimaryKeyAutoIncrement.
- Computed/read-only columns are handled with SaveAs = SaveAs.Ignore.

### Model variants

- A single model can have multiple [StormDbObject] declarations for different db objects (table, view, virtual view, custom SQL).
- Each variant gets its own generated SelectFromX method.

## Examples

### Table model

```csharp
using AltaSoft.Storm.Attributes;

[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "Users", DisplayName = "UsersTable")]
public partial record User
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
    public int UserId { get; set; }

    [StormColumn(ColumnType = ColumnType.PrimaryKey, DbType = UnifiedDbType.Int16)]
    public short BranchId { get; set; }

    [StormColumn(ColumnType = ColumnType.AutoIncrement)]
    public int AutoInc { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? FullName { get; set; }

    [StormColumn(ColumnType = ColumnType.RowVersion | ColumnType.ConcurrencyCheck)]
    public SqlRowVersion RowVersion { get; set; }

    [StormColumn(ColumnType = ColumnType.ConcurrencyCheck)]
    public int Version { get; set; }

    public void AfterLoad(uint partialFlags)
    {
        // Called after entity is loaded from database
    }

    public void BeforeSave(SaveAction action)
    {
        // Called before entity is saved to database
    }
}
```

### View model

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "UsersView", DisplayName = "UsersView", ObjectType = DbObjectType.View)]
public partial record User
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
    public int UserId { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? FullName { get; set; }
}
```

### Table-valued function model

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "users_func", DisplayName = "UsersFunc", ObjectType = DbObjectType.TableValuedFunction)]
public partial record User
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
    public int UserId { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? FullName { get; set; }
}
```

### Stored procedure returning rows

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "GetUsers", ObjectType = DbObjectType.StoredProcedure)]
public partial record User
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
    public int UserId { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? FullName { get; set; }
}
```

### Virtual view model

```csharp
[StormDbObject<MyAppContext>(
    DisplayName = "UsersVirtualView",
    ObjectType = DbObjectType.VirtualView,
    VirtualViewSql = """
        SELECT Id, FullName FROM {%schema%}.Users
        WHERE IsActive = 1
        """)]
public partial record User
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
    public int UserId { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? FullName { get; set; }
}
```

### Custom SQL model

```csharp
[StormDbObject<MyAppContext>(DisplayName = "UsersCustomSql", ObjectType = DbObjectType.CustomSqlStatement)]
public partial record User
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
    public int UserId { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? FullName { get; set; }
}
```

### Model with multiple variants

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "Users", DisplayName = "UsersTable")]
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "UsersView", DisplayName = "UsersView", ObjectType = DbObjectType.View)]
[StormDbObject<MyAppContext>(
    DisplayName = "UsersVirtualView",
    ObjectType = DbObjectType.VirtualView,
    VirtualViewSql = "SELECT * FROM {%schema%}.Users WHERE IsActive = 1")]
public partial record User
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
    public int UserId { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? FullName { get; set; }
}
```

### Procedure with parameters

```csharp
[StormDbObject<MyAppContext>(SchemaName = "test", ObjectName = "InputOutputProc", ObjectType = DbObjectType.StoredProcedure)]
public partial record UserProc
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "Id")]
    public int UserId { get; set; }

    private static int InputOutputProc(
        [StormParameter(ParameterName = "@user_id", DbType = UnifiedDbType.Int32)]
        int? userId,
        [StormParameter(ParameterName = "@result_id", DbType = UnifiedDbType.Int32)]
        out int resultValue,
        [StormParameter(ParameterName = "@io", DbType = UnifiedDbType.Int32)]
        ref int io
    ) => throw new NotImplementedException();
}
```

### From SQL script to Storm model

Given this SQL table definition:

```sql
CREATE TABLE [dbo].[Employees] (
    [EmployeeId] INT PRIMARY KEY IDENTITY(1, 1) NOT NULL,
    [FirstName] VARCHAR(50) NOT NULL,
    [LastName] VARCHAR(50) NOT NULL,
    [Email] VARCHAR(100) UNIQUE NULL,
    [HireDate] DATETIME NOT NULL,
    [Salary] DECIMAL(10, 2) NULL,
    [DepartmentId] INT NOT NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME NOT NULL DEFAULT GETUTCDATE(),
    [ModifiedAt] DATETIME NOT NULL DEFAULT GETUTCDATE(),
    [RowVersion] ROWVERSION NOT NULL
);
```

You would create the corresponding Storm model:

```csharp
using AltaSoft.Storm.Attributes;

[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "Employees", DisplayName = "EmployeesTable")]
public partial record Employee
{
    [StormColumn(ColumnType = ColumnType.PrimaryKeyAutoIncrement, ColumnName = "EmployeeId")]
    public int Id { get; set; }

    [StormColumn(ColumnType = ColumnType.Required, DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string FirstName { get; set; } = null!;

    [StormColumn(ColumnType = ColumnType.Required, DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string LastName { get; set; } = null!;

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? Email { get; set; }

    [StormColumn(ColumnType = ColumnType.Required)]
    public DateTime HireDate { get; set; }

    [StormColumn(Size = 10, Scale = 2)]
    public decimal? Salary { get; set; }

    [StormColumn(ColumnType = ColumnType.Required)]
    public int DepartmentId { get; set; }

    [StormColumn(ColumnType = ColumnType.Required)]
    public bool IsActive { get; set; }

    [StormColumn(ColumnType = ColumnType.Required | ColumnType.AutoUpdate)]
    public DateTime CreatedAt { get; set; }

    [StormColumn(ColumnType = ColumnType.Required | ColumnType.AutoUpdate)]
    public DateTime ModifiedAt { get; set; }

    [StormColumn(ColumnType = ColumnType.RowVersion | ColumnType.ConcurrencyCheck)]
    public SqlRowVersion RowVersion { get; set; }

    public void BeforeSave(SaveAction action)
    {
        if (action == SaveAction.Insert)
        {
            CreatedAt = DateTime.UtcNow;
        }
        ModifiedAt = DateTime.UtcNow;
    }
}
```

**Mapping notes:**
- SQL `INT IDENTITY` → [ColumnType.PrimaryKeyAutoIncrement]
- SQL `VARCHAR(50) NOT NULL` → [ColumnType.Required] with Size=50
- SQL `VARCHAR(100) UNIQUE NULL` → [ColumnType.Unique] if constraint is enforced, or just nullable string
- SQL `DECIMAL(10, 2)` → Size=10, Scale=2
- SQL `BIT` → use `bool` or `bool?` (C# type, no DbType attribute needed — automatic well-known type mapping)
- SQL `DATETIME` or `DATETIME2` → use `DateTime` or `DateTime?` (C# type, no DbType attribute needed — automatic well-known type mapping)
- SQL `ROWVERSION` → SqlRowVersion type with [ColumnType.RowVersion | ColumnType.ConcurrencyCheck]
- SQL `DEFAULT GETUTCDATE()` → [ColumnType.AutoUpdate] to manage at C# level, or handle in database trigger

### Scalar function

SQL scalar function:

```sql
CREATE FUNCTION [dbo].[CalculateBonus] (
    @salary DECIMAL(10, 2),
    @yearsOfService INT
)
RETURNS DECIMAL(10, 2)
AS
BEGIN
    RETURN @salary * (0.05 + @yearsOfService * 0.01);
END;
```

Storm model:

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "CalculateBonus", ObjectType = DbObjectType.TableValuedFunction)]
public partial record BonusCalculation
{
    private static decimal CalculateBonus(
        [StormParameter(ParameterName = "@salary", DbType = UnifiedDbType.Decimal)]
        decimal salary,
        [StormParameter(ParameterName = "@yearsOfService", DbType = UnifiedDbType.Int32)]
        int yearsOfService
    ) => throw new NotImplementedException();
}
```

### Stored procedure with input parameters only

SQL stored procedure:

```sql
CREATE PROCEDURE [dbo].[GetEmployeesByDepartment]
    @departmentId INT,
    @isActive BIT = 1
AS
BEGIN
    SELECT EmployeeId, FirstName, LastName, Email, HireDate
    FROM [dbo].[Employees]
    WHERE DepartmentId = @departmentId
      AND IsActive = @isActive;
END;
```

Storm model:

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "GetEmployeesByDepartment", ObjectType = DbObjectType.StoredProcedure)]
public partial record EmployeeResult
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public int EmployeeId { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string? FirstName { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string? LastName { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? Email { get; set; }

    [StormColumn(DbType = UnifiedDbType.DateTime)]
    public DateTime HireDate { get; set; }

    private static void GetEmployeesByDepartment(
        [StormParameter(ParameterName = "@departmentId", DbType = UnifiedDbType.Int32)]
        int departmentId,
        [StormParameter(ParameterName = "@isActive", DbType = UnifiedDbType.Bit)]
        byte isActive = 1
    ) => throw new NotImplementedException();
}
```

### Stored procedure with input and output parameters

SQL stored procedure:

```sql
CREATE PROCEDURE [dbo].[UpdateEmployeeSalary]
    @employeeId INT,
    @newSalary DECIMAL(10, 2),
    @oldSalary DECIMAL(10, 2) OUTPUT,
    @updateCount INT OUTPUT
AS
BEGIN
    SELECT @oldSalary = Salary FROM [dbo].[Employees] WHERE EmployeeId = @employeeId;
    
    UPDATE [dbo].[Employees]
    SET Salary = @newSalary
    WHERE EmployeeId = @employeeId;
    
    SET @updateCount = @@ROWCOUNT;
END;
```

Storm model:

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "UpdateEmployeeSalary", ObjectType = DbObjectType.StoredProcedure)]
public partial record UpdateSalaryProc
{
    private static void UpdateEmployeeSalary(
        [StormParameter(ParameterName = "@employeeId", DbType = UnifiedDbType.Int32)]
        int employeeId,
        [StormParameter(ParameterName = "@newSalary", DbType = UnifiedDbType.Decimal)]
        decimal newSalary,
        [StormParameter(ParameterName = "@oldSalary", DbType = UnifiedDbType.Decimal, Direction = ParameterDirection.Output)]
        out decimal oldSalary,
        [StormParameter(ParameterName = "@updateCount", DbType = UnifiedDbType.Int32, Direction = ParameterDirection.Output)]
        out int updateCount
    ) => throw new NotImplementedException();
}
```

### Stored procedure with return value

SQL stored procedure:

```sql
CREATE PROCEDURE [dbo].[DeleteEmployee]
    @employeeId INT
AS
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Employees] WHERE EmployeeId = @employeeId)
    BEGIN
        RETURN -1; -- Employee not found
    END
    
    DELETE FROM [dbo].[Employees] WHERE EmployeeId = @employeeId;
    
    RETURN 0; -- Success
END;
```

Storm model:

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "DeleteEmployee", ObjectType = DbObjectType.StoredProcedure)]
public partial record DeleteEmployeeProc
{
    private static int DeleteEmployee(
        [StormParameter(ParameterName = "@employeeId", DbType = UnifiedDbType.Int32)]
        int employeeId
    ) => throw new NotImplementedException();
}
```

### Table-valued function returning rows

SQL table-valued function:

```sql
CREATE FUNCTION [dbo].[GetEmployeesByHireDateRange] (
    @startDate DATETIME,
    @endDate DATETIME
)
RETURNS TABLE
AS
RETURN
(
    SELECT EmployeeId, FirstName, LastName, Email, HireDate
    FROM [dbo].[Employees]
    WHERE HireDate BETWEEN @startDate AND @endDate
);
```

Storm model:

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "GetEmployeesByHireDateRange", DisplayName = "EmployeesByHireDate", ObjectType = DbObjectType.TableValuedFunction)]
public partial record EmployeeResult
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public int EmployeeId { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string? FirstName { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string? LastName { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? Email { get; set; }

    [StormColumn(DbType = UnifiedDbType.DateTime)]
    public DateTime HireDate { get; set; }

    private static void GetEmployeesByHireDateRange(
        [StormParameter(ParameterName = "@startDate", DbType = UnifiedDbType.DateTime)]
        DateTime startDate,
        [StormParameter(ParameterName = "@endDate", DbType = UnifiedDbType.DateTime)]
        DateTime endDate
    ) => throw new NotImplementedException();
}
```

### View model with joins

SQL view:

```sql
CREATE VIEW [dbo].[EmployeeDepartmentView]
AS
SELECT 
    e.EmployeeId,
    e.FirstName,
    e.LastName,
    e.Salary,
    d.DepartmentName,
    d.Budget
FROM [dbo].[Employees] e
INNER JOIN [dbo].[Departments] d ON e.DepartmentId = d.DepartmentId
WHERE e.IsActive = 1;
```

Storm model:

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "EmployeeDepartmentView", DisplayName = "EmployeeDepartmentView", ObjectType = DbObjectType.View)]
public partial record EmployeeDepartmentInfo
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey, ColumnName = "EmployeeId")]
    public int Id { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string? FirstName { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string? LastName { get; set; }

    [StormColumn(DbType = UnifiedDbType.Decimal, Size = 10, Scale = 2)]
    public decimal? Salary { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string? DepartmentName { get; set; }

    [StormColumn(DbType = UnifiedDbType.Decimal, Size = 15, Scale = 2)]
    public decimal? Budget { get; set; }
}
```
