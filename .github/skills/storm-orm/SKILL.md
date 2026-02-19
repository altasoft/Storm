---
name: storm-orm
description: Storm ORM (AltaSoft.Storm) usage for .NET with MSSQL. Use when the user asks to write Storm ORM code, map tables to classes or classes to tables, query/insert/update/delete/merge data, use transactions, bulk operations, or call stored procedures/functions in this repo.
---

# Storm ORM Assistant

## Sources

References are organized by operation type. Load the relevant reference as needed:
- **Starting point**: See references/model-binding.md for attribute mapping or references/queries.md for query operations
- **Method signature unclear?**: Ask user for the generated method signature 
- **Determine generated method names**: See references/model-binding.md for naming rules based on StormDbObject attributes

## Workflow

1. **Check for StormContext class**: Search the project for a class inheriting from StormContext (pattern: `class.*: StormContext`).
   - If it doesn't exist, ask the user for a context name following the pattern: ProjectName + "Context" (e.g., TransactionsContext, PaymentsContext, OrderManagementContext).
   - Create the context class as simple sealed partial class (minimal syntax).
   - Verify the context will be registered in program.cs using `StormManager.Initialize` and `AddStormContext<T>`.
2. Confirm the target database (MSSQL in this skill), the connection string source, and the StormContext type.
3. Identify the database object type: table, view, stored procedure, function, virtual view, or custom SQL.
4. Identify the operation: select, insert, update, delete, merge, bulk insert, transaction, or partial load.
5. Use a transaction scope when multiple writes must be consistent (multiple updates, inserts, deletes, or batch operations).
6. Ask for missing details: schema name, primary key, identity, nullability, and required column sizes.
7. Determine the generated method name from the StormDbObject attributes (see references/model-binding.md for naming rules: SelectFromX, InsertIntoX, UpdateX, DeleteFromX, MergeIntoX, BulkInsertIntoX, ExecuteX, ExecuteScalarX).
8. **Context creation**: Use the identified/created StormContext with the **parameterless constructor** (assuming it's registered in program.cs). Preferred: `await using var context = new YourContext();` NOT `new YourContext(_connectionString)`. The connection string is automatically retrieved from the app startup registration.
9. Output C# code only, unless the user explicitly asks for explanation.

## Key Tips

- **Context creation pattern**: Create context instances as `await using var context = new YourContext();` after contexts are registered in program.cs via `AddStormContext<T>`. Connection string is automatically retrieved.
- **Well-known scalar types**: DateTime (C#) automatically maps to `datetime2` (SQL Server) without needing `[StormColumn(DbType = UnifiedDbType.DateTime2)]`. Use `DateTime` or `DateTime?` directly in the model.
- **Omit DbType for standard types**: Don't specify DbType for automatic mappings like `int` → `int`, `bool` → `bit`, `decimal` → `decimal`, `Guid` → `uniqueidentifier`. Storm handles these automatically.
- Only use `[StormColumn]` when you need to specify ColumnType, SaveAs, Size, Precision, Scale, or when ColumnName differs from property name.

- Provide code in a single C# code block with no extra prose by default.
- Use async APIs: GetAsync, ListAsync, GoAsync, CountAsync, ExistsAsync.
- Use WithTracking only when change tracking is required.
- Use WithConcurrencyCheck or WithoutConcurrencyCheck explicitly for updates that depend on concurrency behavior.
- Format Storm method chains with one method per line and proper indentation:
  ```csharp
  await context
      .SelectFromUsersTable()
      .Where(x => x.Id > 10)
      .OrderBy(User.OrderByKey)
      .ListAsync()
      .ConfigureAwait(false);
  ```

## References (load as needed)
- Initialization and context setup: references/initialization.md
- Model binding and attribute mapping: references/model-binding.md
- Queries and projections: references/queries.md
- Inserts and bulk inserts: references/inserts.md
- Updates and patches: references/updates.md
- Deletes: references/deletes.md
- Merges: references/merges.md
- Transactions and scopes: references/transactions.md
- Stored procedures and functions: references/sprocs-functions.md
- Custom SQL and virtual views: references/custom-sql-virtual-views.md
- Model generation from database: references/model-generation.md
- SQL script generation: references/sql-generation.md
