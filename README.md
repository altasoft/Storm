# AltaSoft.Storm — Modern, High-Performance ORM for .NET

[![NuGet](https://img.shields.io/nuget/v/AltaSoft.Storm.MsSql?style=for-the-badge)](https://www.nuget.org/packages/AltaSoft.Storm.MsSql)
[![Dot NET 8+](https://img.shields.io/static/v1?label=DOTNET&message=8%2B&color=0c3c60&style=for-the-badge)](https://dotnet.microsoft.com)
[![Dot NET 9+](https://img.shields.io/static/v1?label=DOTNET&message=9%2B&color=0c3c60&style=for-the-badge)](https://dotnet.microsoft.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](LICENSE)

---

## Table of Contents

- [Introduction](#introduction)
- [Philosophy & Design Goals](#philosophy--design-goals)
- [Supported Databases](#supported-databases)
- [Feature Overview](#feature-overview)
- [Installation & Setup](#installation--setup)
- [Model Definition](#model-definition)
- [Querying & CRUD](#querying--crud)
- [Update & Patch Examples](#update--patch-examples)
- [Batch Operations & Bulk Insert](#batch-operations--bulk-insert)
- [Partial Loading](#partial-loading)
- [Unit of Work & Transactions](#unit-of-work--transactions)
- [Stored Procedures & Scalar Functions](#stored-procedures--scalar-functions)
- [Virtual Views & Custom SQL](#virtual-views--custom-sql)
- [Domain Primitives](#domain-primitives)
- [Serialization: JSON & XML](#serialization-json--xml)
- [SQL Compression/Decompression](#sql-compressiondecompression)
- [Change Tracking & Concurrency](#change-tracking--concurrency)
- [Logging & Exception Handling](#logging--exception-handling)
- [Extensibility & Advanced Configuration](#extensibility--advanced-configuration)
- [Enum Storage: Store Enums as Strings](#enum-storage-store-enums-as-strings)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)
- [FAQ](#faq)

---

## Introduction

**AltaSoft.Storm** is a lightning-fast, source-generator-powered ORM for .NET, designed to bring the best of type safety, performance, and developer ergonomics to your data layer. Storm eliminates runtime reflection, automates change tracking, and supports rich features for modern enterprise applications.

---

## Philosophy & Design Goals

- **Performance First:** Uses C# source generators for compile-time bindings; no runtime reflection.
- **Developer Happiness:** Clear, type-safe APIs and auto-generated helpers for all CRUD and advanced scenarios.
- **Extensibility:** Easily supports new databases, serialization providers, and custom behaviors.
- **Transparency:** No magic or “black box” behavior; everything is open and documented.

---

## Supported Databases

- **MSSQL** — Fully supported, production-ready.
- **Other DBs:** PostgreSQL, MySQL, etc., planned for future releases. Community contributions welcome!

---

## Feature Overview

- **Source Generator Bindings** — Tables, views, stored procedures, functions, virtual views, custom SQL.
- **Automatic Change Tracking** — Efficient IL weaving for property change detection.
- **Batch Operations & Bulk Insert** — High-speed batch updates/inserts.
- **Partial Loading** — Load only the fields you need, including nested/detail tables.
- **Unit of Work & Transactions** — Robust transaction management.
- **Stored Procedures & Scalar Functions** — Strongly-typed execution and result mapping.
- **Virtual Views & Custom SQL** — Map models to SQL views, virtual views, or arbitrary SQL.
- **Domain Primitives Support** — Seamless integration with [AltaSoft.DomainPrimitives](https://github.com/altasoft/DomainPrimitives).
- **Serialization (JSON/XML)** — Save/load complex properties as JSON (preferred) or XML.
- **SQL Compression/Decompression** — Efficiently store large strings as compressed data.
- **Change Tracking & Concurrency** — Optimistic concurrency and dirty-checking.
- **Logging & Error Handling** — Plug in your own logger, all errors use StormException.
- **Table Hints, Schema Customization, Connection Management** — Advanced configuration options.
- **Open Source, MIT Licensed, Community-Driven.**

---

## Installation & Setup

### Prerequisites

- .NET 8 or higher

### NuGet Installation

Add the package to your project:

```xml
<ItemGroup>
  <PackageReference Include="AltaSoft.Storm.MsSql"/>
  <PackageReference Include="AltaSoft.Storm.Generator.MsSql">
	    <PrivateAssets>all</PrivateAssets>
	    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
</ItemGroup>
<PropertyGroup>
  <DefineConstants>$(DefineConstants);STORM_MSSQL</DefineConstants>
</PropertyGroup>
```

### Initialization

```csharp
public sealed class  MyAppContext:StormContext;

if (!StormManager.IsInitialized)
{
    StormManager.Initialize(new MsSqlOrmProvider(), configuration =>
    {
        configuration.AddStormContext<MyAppContext>(dbConfig =>
        {
            dbConfig.UseConnectionString("your-connection-string");
            dbConfig.UseDefaultSchema("dbo");
        });
    });
}
```

---

## Model Definition

Bind C# classes to DB objects with rich attributes:

```csharp
[StormDbObject<MyAppContext>(SchemaName = "dbo", ObjectName = "Users", DisplayName = "User Entity")]
public sealed partial class User
{
    [StormColumn(ColumnType = ColumnType.PrimaryKeyAutoIncrement)]
    public int Id { get; set; }

    [StormColumn(DbType = UnifiedDbType.String, Size = 200)]
    public string Name { get; set; }

    public DateOnly BirthDate { get; set; }

    [StormColumn(DbType = UnifiedDbType.Json)]
    public List<Role> Roles { get; set; }

    // Domain primitive support (see below)
    public UserId UserId { get; set; }
}
```

### Supported Attributes

- **StormDbObject**: Binds to table/view/SP/function; customize schema, name, context, display name, update mode.
- **StormColumn**: Controls DB type, size, precision, scale, name, save/load options, detail table name, concurrency, tracking, etc.

---

## Querying & CRUD

All CRUD and query methods are auto-generated by Storm:

```csharp
await using var context = new MyAppContext("your-connection-string");

// Get by primary key
var user = await context.SelectFromUsersTable(1).GetAsync();

// List all
var users = await context.SelectFromUsersTable().ListAsync();

// Filtering, ordering, pagination
var filtered = await context.SelectFromUsersTable()
    .Where(x => x.BirthDate < new DateOnly(2010, 1, 1))
    .OrderBy(User.OrderBy.BirthDate_Desc)
    .Top(10)
    .ListAsync();

// Partial projection
var names = await context.SelectFromUsersTable().ListAsync(x => x.Name, x => x.BirthDate);
```

## Update & Delete

```csharp
// Change tracking update
var user = await context.SelectFromUsersTable(1).WithTracking().GetAsync();
user.Name = "Updated Name";
await context.UpdateUsersTable().Set(user).GoAsync();

// Delete by key
await context.DeleteFromUsersTable(1).GoAsync();
```

---

## Update & Patch Examples

AltaSoft.Storm supports expressive update and patch operations. You can update specific fields directly:

```csharp
// Update a single field by key
await context.UpdatePersonTable(10).Set(x => x.Name, "NewName").GoAsync();

// Update multiple fields by key
await context.UpdatePersonTable(10)
    .Set(x => x.Name, "NewName")
    .Set(x => x.Age, 42)
    .GoAsync();

// Patch by condition
await context.UpdatePersonTable()
    .Where(x => x.Age < 18)
    .Set(x => x.IsMinor, true)
    .GoAsync();

// Set by key property
await context.UpdatePersonTable().Set(x => x.Id, 10).Set(x => x.Name, "Updated").GoAsync();
```

---

## Batch Operations & Bulk Insert

### Batch Update

```csharp
var batchUsers = new[] { user1, user2, user3 };
await context.UpdateUsersTable().Set(batchUsers).GoAsync();
```

### Bulk Insert

```csharp
await context.BulkInsertIntoUsersTable().Values(batchUsers).GoAsync();
```

---

## Partial Loading

Use partial load flags to optimize queries:

```csharp
var userList = await context.SelectFromUsersTable()
    .Partially(User.PartialLoadFlags.FullName)
    .OrderBy(User.OrderByKey)
    .ListAsync();
```

Load detail tables or nested objects as needed.

---

## Unit of Work & Transactions

Manage complex operations atomically:

```csharp
using var uow = UnitOfWork.Create();
await using var tx = await uow.BeginAsync("your-connection-string", CancellationToken.None);

var context = new MyAppContext("your-connection-string");

// Batch update
await context.UpdateUsersTable().Set(usersToUpdate).GoAsync();

await tx.CompleteAsync(CancellationToken.None);
```

---

## Stored Procedures & Scalar Functions

Call stored procedures and functions with type safety:

```csharp
// Scalar function
var result = await context.ExecuteScalarFunc(userId, branchId).GetAsync();

// Stored procedure
var procResult = await context.ExecuteInputOutputProc(userId, ioValue).GoAsync();
// procResult contains output parameters, rows affected, etc.
```

Parameters and results are mapped automatically.

---

## Virtual Views & Custom SQL

Map models to SQL views, virtual views, or custom SQL statements:

```csharp
// Virtual view
var specialUsers = await context.SelectFromUsersVirtualView().ListAsync();

// Custom SQL
var customSql = "SELECT * FROM dbo.Users WHERE IsActive = 1";
var activeUsers = await context.SelectFromUsersCustomSql(customSql).ListAsync();
```

---

## Domain Primitives

Storm seamlessly supports [AltaSoft.DomainPrimitives](https://github.com/altasoft/DomainPrimitives):

```csharp
public class User
{
    public UserId Id { get; set; } // auto-mapped to underlying type
}
```
No extra configuration needed.

---

## Serialization: JSON & XML

**JSON is preferred** for complex object storage:

```csharp
[StormColumn(DbType = UnifiedDbType.Json)]
public List<Role> Roles { get; set; }
```

**XML** is also supported:

```csharp
[StormColumn(DbType = UnifiedDbType.Xml)]
public Profile ProfileXml { get; set; }
```

Plug in custom serialization providers if needed:

```csharp
StormManager.Initialize(
    new MsSqlOrmProvider(), 
    configuration => { /* ... */ },
    jsonSerializationProvider: new MyJsonProvider(), 
    xmlSerializationProvider: new MyXmlProvider()
);
```

---

## SQL Compression/Decompression

Efficiently store large strings as compressed binary:

```csharp
[StormColumn(DbType = UnifiedDbType.VarBinary, SaveAs = SaveAs.CompressedString)]
public string BigString { get; set; }
```

Storm will compress on save and decompress on retrieval automatically.

---

## Change Tracking & Concurrency

Change tracking is automatic (IL weave); use it for dirty-checking and efficient updates.

```csharp
var user = await context.SelectFromUsersTable(1).WithTracking().GetAsync();
user.Name = "New Name";
if (user.IsDirty())
    await context.UpdateUsersTable().Set(user).GoAsync();
```

Support for concurrency check and optimistic locking:

```csharp
[StormColumn(ColumnType = ColumnType.ConcurrencyCheck | ColumnType.RowVersion)]
public byte[] RowVersion { get; set; }
```

---

## Logging & Exception Handling

Plug in any `ILogger` implementation for full logging. All ORM errors use `StormException` for clear diagnostics.

```csharp
StormManager.SetLogger(myLogger);
```

---

## Extensibility & Advanced Configuration

- **Custom Table/View Names:** Use `SchemaName`, `ObjectName`, and `DisplayName` in `[StormDbObject]`.
- **Detail Table Mapping:** Use `DetailTableName` in `[StormColumn]` for one-to-many relationships.
- **Concurrency & Tracking:** Use appropriate column types.
- **Custom Exception Handling:** All ORM errors throw `StormException`.
- **Custom Serialization Providers:** Plug in your own.
- **Source Generators:** Easily extend for new DBs and behaviors.

---

## Enum Storage: Store Enums as Strings

Storm supports storing enums as strings in the database for better readability and schema evolution. To enable this, use the `[StormStringEnum<EnumType, ConverterType>(maxLength)]` attribute on your enum definition, and annotate your model property with `[StormColumn(SaveAs = SaveAs.String)]`:

```csharp
[StormStringEnum<RgbColor, RgbColorExt>(16)]
public enum RgbColor : sbyte {
    Red,
    Green,
    Blue,
    White,
    Black
}

public sealed class RgbColorExt : IStormStringToEnumConverter<RgbColor>
{
    public static string ToDbString(RgbColor value)
    {
        return value switch
        {
            RgbColor.Red => "#FF0000",
            RgbColor.Green => "#00FF00",
            RgbColor.Blue => "#0000FF",
            RgbColor.White => "#FFFFFF",
            RgbColor.Black => "#000000",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }

    public static RgbColor FromDbString(string value)
    {
        return value.ToUpper() switch
        {
            "#FF0000" => RgbColor.Red,
            "#00FF00" => RgbColor.Green,
            "#0000FF" => RgbColor.Blue,
            "#FFFFFF" => RgbColor.White,
            "#000000" => RgbColor.Black,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}

public class Car {
    [StormColumn(SaveAs = SaveAs.String)]
    public RgbColor Color { get; set; } = RgbColor.Blue;
}
```

This configuration ensures that the enum values are stored as strings in the database, rather than their underlying integer values. This approach improves schema clarity and makes future changes to enum values safer.

---

## Contributing

AltaSoft.Storm is MIT licensed and welcomes all contributions! Open issues, submit PRs, and help us build the future of .NET ORM.

---

## License

MIT

---

## Contact

For support, questions, or additional info:
- [GitHub Issues](https://github.com/altasoft/Storm/issues)
- Discussions tab on GitHub

---

## FAQ

### Can I use Storm with databases other than MSSQL?
Currently, only MSSQL is fully supported. Other databases are planned for future releases. Contributions are welcome!

### How do I add a new model or context?
Just use `[StormDbObject<MyContext>]` and Storm will generate all extension methods and helpers automatically.

### How are domain primitives handled?
Any type implementing the domain primitive pattern is auto-mapped to its underlying DB type.

### How do I bulk insert or batch update?
Use the generated `BulkInsertInto...` or batch update methods—see code samples above.

### How do I handle transactions?
Use `UnitOfWork.Create()` and transaction helpers.

### How do I report bugs or request features?
Open an issue or discussion on GitHub!

---

## Related Projects

- [AltaSoft.DomainPrimitives](https://github.com/altasoft/DomainPrimitives)

---

_AltaSoft.Storm — Fast, Modern, and Open Source ORM for .NET_
