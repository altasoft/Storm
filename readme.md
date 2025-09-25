# AltaSoft.Storm- A Sleek and Efficient ORM Solution for C#

[![Version](https://img.shields.io/nuget/v/AltaSoft.Storm?label=Version&color=0c3c60&style=for-the-badge&logo=nuget)](https://www.nuget.org/profiles/AltaSoft)
[![Dot NET 7+](https://img.shields.io/static/v1?label=DOTNET&message=7%2B&color=0c3c60&style=for-the-badge)](https://dotnet.microsoft.com)

# Table of Contents

- [Introduction](#introduction)
- [Key Features](#key-features)
- [Generator Features](#generator-features)
- [Supported Underlying types](#supported-underlying-types)
- [Getting Started](#getting-started)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Creating your Domain type](#creating-your-domain-type)
- [Json Conversion](#json-conversion)
- [Contributions](#contributions)
- [Contact](#contact)
- [License](#license)

## Introduction

Welcome to **AltaSoft.Storm** - The lightweight and lightning-fast C# ORM solution that takes your development to new heights. With its built-in property change tracking, seamless multiple table and view binding, and robust support for executing stored procedures, functions, and beyond, Storm stands as your ultimate one-stop solution for all your ORM needs. Plus, with integrated source generators, Storm ensures optimal performance and efficiency in your data access layer. Dive into the full range of features [here](#all-features).

## Supported Databases 

Currently **Storm** Only supports the following Databases:
* MSSQL - Fully supported
* PostgreSQL - Not yet supported
* MySql - Not yet supported

## Key Features 
* Storm leverages C# source generators instead of reflection, enhancing performance in C# code for improved database access.
* Storm employs a lightweight IL Weave mechanism to capture changed properties, ensuring efficient updates.
* Storm automatically includes primary keys to simplify the retrieval of an object. Also generates a specific OrderBy values for each properties
* Connect a C# class seamlessly to a database entity for effortless reading, adding, and updating.
* Connect a C# class to a view and a virtual view for effortless reading, adding, and updating.
* Connect a C# class to StoredProcedure
* Connect a C# class to TableValuedFunction
* Connect a C# class to ScalarValuedFunction
* Utilize multiple StormContexts to execute CRUD operations on your entities effortlessly. Additionally, the Context offers a variety of extension methods to streamline development. 
* Storm has a built in support for [AltaSoft.DomainPrimitives] (https://github.com/altasoft/DomainPrimitives)
* Storm can save a complex objects as a json in the database
 
## Getting Started

### Prerequisites
*	.NET 8 or higher
*	NuGet Package Manager

### Installation

To use **AltaSoft.Storm**, install the following NuGet package:

1. `AltaSoft.Storm.MsSql`

In your project file add references as follows:

```xml
    <ItemGroup>
    <PackageReference Include="AltaSoft.Storm.MsSql" Version="x.x.x" />
    </ItemGroup>
	<PropertyGroup>
		<DefineConstants>$(DefineConstants);STORM_MSSQL</DefineConstants>  <!--specifies that storm uses MSSQL as the database-->
	</PropertyGroup>
```


## **Creating a Database Entity**

```csharp
[StormDbObject]
public sealed partial class User
{
    [StormColumn(ColumnType = ColumnType.PrimaryKeyAutoIncrement)]
    public int Id { get; set; }

    [StormColumn(DbType = UnifiedDbType.String, Size = 200)]
    public string Name { get; set; }

    [StormColumn(DbType = UnifiedDbType.String, Size = 200)]
    public string LastName { get; set; }

    public DateOnly BirthDate { get; set; }
}
```

Please read the further details for [StormDbObjectAttribute](#stormdbobjectattribute) and [StormColumnAttribute](#stormcolumnattribute) for the best result.

## Querying a Database Entity
Storm automatically Generates a list of querying methods for each database entity

### SelectFromUser Conditions
1. **Where** -> To apply a where condition to the query
2. **OrderBy** -> To order a query based on the properties provided
3. **Top** -> To specify how many items to retrieve
4. **Skip** -> To specify how many items to skip
5. **WithCloseConnection** -> To automatically close the sql connection
6. **WithTableHints** -> To specify a hint such as NoLock. Please refer to [available Hints](#available-table-hints)
7. **WithTracking** ->  To specify if an Property change tracking should be enabled when an object is loaded
8. **WithCommandTimeOut** -> To specify a commandTimeout 

### SelectFromUser Queries
1. **GetAsync** -> Retrieves the first result of a query
2. **ListAsync** -> Lists the result of a query
3. **StreamAsyn** -> Streams the result of a query to IAsyncEnumerable
4. **CountAsync** -> Counts the items of a query
5. **ExitsAsync** -> Checks if an item exists

## SelectFromUser (with primary key specified in method parameter)
1. **GetAsync** -> Retrieves the first result of a query
2. **CountAsync** -> Counts the items of a query
3. **ExitsAsync** -> Checks if an item exists
4. **WithCloseConnection** -> To automatically close the sql connection
5. **WithTableHints** -> To Specify a hint such as NoLock. Please refer to [available Hints](#available-table-hints)
6. **WithTracking** ->  To Specify if an Property change tracking should be enabled when an object is loaded
7. **WithCommandTimeOut** -> To specify a commandTimeout 


```csharp
await using var context = new StormContext();
var user = await context.SelectFromUser(1).GetAsync();                  // retrieving a user with Id 1. Storm automatically generates parameters based on primary keys
var users = await context.SelectFromUser().ListAsync();                 // lists all users from the database
var streamedUsers = await context.SelectFromUser().StreamAsync();       // streams all users from the database

var filteredUsers = await context.SelectFromUser()  
    .Where(x => x.BirthDate < new DateOnly(2010, 1, 1))                 // filters users based on their BirthDate
    .OrderBy(User.OrderBy.BirthDate_Desc)                               // orders the query by Birthday decreasing 
    .Top(10)                                                            // takes the first 10 items
    .WithTableHints(StormTableHints.NoLock)                             // specifies that No Lock hint should be used with the query
    .WithCloseConnection()                                              // closes the connection
    .ListAsync();                                                       // List the users
```

It's also possible to perform a partial data retrieval with **Storm** 
```csharp
 var users = await context.SelectFromUser.ListAsync(x => x.Name, x => x.LastName);   // lists only Name and LastName of from sql query
```

## Adding & Updating 

## Insert an object into database

It's very simple to add an object to a database with Storm
```csharp

   await using var context = new StormContext();

   var user = new User
   {
       BirthDate = new DateOnly(1997, 1, 1),
       Name = "Test",
       LastName = "Test",
   };
   await context.InsertIntoUser().Values(user).GoAsync();  //inserts a value or IEnumerable of values into the database
```
### InsertIntoUser has the following capabilities
1. **WithCloseConnection** -> To automatically close the sql connection
2. **WithCommandTimeOut** -> To specify a commandTimeout 

## update an object into database 
### With a known object
```csharp
        await using var context = new StormContext();

        var user = await context.SelectFromUser(1).WithTracking().GetAsync();

        user.Name = "New Name";
        user.LastName = "New Last Name";

        await context.UpdateUser().Set(user).GoAsync();
```

### Unkown object

```csharp
  await context
      .UpdateUser()
      .Set(x => x.Name, "NameBefore2000")
      .Set(x => x.LastName, "LastNameBefore2000")
      .Where(x => x.BirthDate < new DateOnly(2020, 1, 1)).GoAsync(); 
```

## MergeInto

Storm also supports MergeInto funcitonality. Either by first trying to insert and then update or opposite.
```csharp
await context.MergeIntoUser().UpdateOrInsert(user).GoAsync();  //first tries to update if fails then inserts
await context.MergeIntoUser().InsertOrUpdate(user).GoAsync(); // first tries to insert if fails then updates
```

## **StormDbObjectAttribute** 
StormDbObjectAttribute offers many properties that can help you customize your db object. 

 1. ContextType -> Type of the Storm context. Default value is "AltaSoft.Storm.StormContext".
 2. SchemaName -> The database schema to use if not specified the default schema will be used.
 3. ObjectName -> This property defines the table name in the database that the class will be mapped to. If left null, the pluralized class name is used
 4. [DbObjectType](#dbobjecttype) -> the database object, default value is Table
 5. DisplayName -> a name to be displayed in the context, if null class name will be used
 6. UpdateMode -> Available values are ChangeTracking, UpdateAll, NoUpdates. By default ChangeTracking is used

## DbObjectType
1. `Table` -> Represents a database table
2. `View` -> Represents a database view.
3. `VirtualView` -> Represents a virtual (defined in code) database view.
4. `StoredProcedure` -> Represents a stored procedure in the database.
5. `TableValuedFunction` -> Represents a table-valued function in the database.
6. `ScalarValuedFunction` -> Represents a scalar-valued function in the database.

## StormColumnAttribute
- `DbType`-> type of the database column/parameter. The default is `UnifiedDbType.Default`. Please refer to [UnifiedDbType](#unifieddbtype)
- `Size`-> the size of the database column/parameter. This is typically used for specifying the size of string parameters.
- `Precision`-> the precision of the database column/parameter. This is typically used for decimal parameters.
- `Scale`-> the scale of the database column/parameter. This is typically used for decimal parameters to specify the number of digits to the right of the decimal point.
- `ColumnName`-> the name of the database column. If not set, the property name is used as the column name.
- `SaveAs`-> the save behavior of the column in the database. The default is `SaveAs.Default`.
- `LoadWithFlags`-> a value indicating whether to load this column with flags.
- `ColumnType`-> the type of the column. The default is `ColumnType.Default`. Please refer to [ColumnType](#columntype)
- `DetailTableName`-> the name of the detail table if this property is a reference to another table. This is typically used for foreign key relationships.


## UnifiedDbType

Unified db type helps correctly bind a c# type to datase type.

| UnifiedDbType            | Description                                                                                          | SQL Server      | PostgreSQL      | MySQL            | SQLite           | Oracle           |
|--------------------------|------------------------------------------------------------------------------------------------------|-----------------|-----------------|------------------|------------------|------------------|
| Default                  | A default placeholder value, used to represent an unspecified data type.                             |                 |                 |                  |                  |                  |
| Boolean                  | A simple type representing Boolean values of `true` or `false`.                                        | Bit             | Boolean         | TinyInt(1)       | INTEGER          | NUMBER(1)        |
| UInt8                    | An 8-bit unsigned integer ranging in value from 0 to 255.                                              | TinyInt         | SmallInt        | Unsigned TinyInt | INTEGER          | NUMBER(3)        |
| Int8                     | An integral type representing signed 8-bit integers with values between -128 and 127.                  | SmallInt        | "char"          | TinyInt          | INTEGER          | NUMBER(3)        |
| UInt16                   | An integral type representing unsigned 16-bit integers with values between 0 and 65535.                | Int             | Integer         | Unsigned SmallInt| INTEGER          | NUMBER(5)        |
| Int16                    | An integral type representing signed 16-bit integers with values between -32768 and 32767.              | SmallInt        | SmallInt        | SmallInt         | INTEGER          | NUMBER(5)        |
| UInt32                   | An integral type representing unsigned 32-bit integers with values between 0 and 4294967295.           | Bigint          | Bigint          | Unsigned Int     | INTEGER          | NUMBER(10)       |
| Int32                    | An integral type representing signed 32-bit integers with values between -2147483648 and 2147483647.   | Int             | Integer         | Int              | INTEGER          | NUMBER(10)       |
| UInt64                   | An integral type representing unsigned 64-bit integers with values between 0 and 18446744073709551615. | Not supported   | Numeric         | Unsigned Bigint  | Not supported    | Not supported    |
| Int64                    | An integral type representing signed 64-bit integers with values between -9223372036854775808 and 9223372036854775807. | Bigint   | Bigint          | Bigint           | INTEGER          | NUMBER(19)       |
| AnsiChar                 | A non-Unicode character.                                                                             | Char            | Char            | Char             | TEXT             | CHAR             |
| Char                     | A Unicode character.                                                                                 | NChar           | Char            | Char             | TEXT             | NCHAR            |
| AnsiString               | A variable-length stream of non-Unicode characters ranging between 1 and 8,000 characters.            | VarChar         | Varchar         | VarChar          | TEXT             | VARCHAR2         |
| String                   | A variable-length stream of Unicode characters ranging between 1 and 4,000 characters.                 | NVarChar        | Text            | VarChar          | TEXT             | NVARCHAR2        |
| AnsiStringFixedLength    | A fixed-length stream of non-Unicode characters.                                                     | Char            | Char            | Char             | TEXT             | CHAR             |
| StringFixedLength        | A fixed-length string of Unicode characters.                                                         | NChar           | Char            | Char             | TEXT             | NCHAR            |
| Currency                 | A currency value ranging from -2^63 to 2^63-1 with an accuracy to a ten-thousandth of a currency unit. | Money           | Money           | Decimal          | NUMERIC          | NUMBER           |
| Single                   | A floating point type with a precision of 7 digits.                                                  | Real            | Real            | Float            | REAL             | BINARY_FLOAT     |
| Double                   | A floating point type with a precision of 15-16 digits.                                               | Float           | Double Precision| Double           | REAL             | BINARY_DOUBLE    |
| Decimal                  | A type representing values with 28-29 significant digits.                                             | Decimal         | Numeric         | Decimal          | NUMERIC          | NUMBER           |
| SmallDateTime            | A type representing date and time values.                                                            | SmallDateTime   | Timestamp       | DateTime         | TEXT             | DATE             |
| DateTime                 | A type representing date and time data.                                                              | DateTime2       | Timestamp       | DateTime         | TEXT             | TIMESTAMP        |
| DateTimeOffset           | A type representing date and time data with time zone awareness.                                       | DateTimeOffset | Timestamptz     | DateTime         | TEXT             | TIMESTAMP WITH TIME ZONE |
| Date                     | A type representing date values.                                                                     | Date            | Date            | Date             | TEXT             | DATE             |
| Time                     | A type representing time values.                                                                     | Time            | Time            | Time             | TEXT             | DATE             |
| Guid                     | A globally unique identifier (or GUID).                                                               | UniqueIdentifier| Uuid            | Char(36)         | TEXT             | RAW(16)          |
| AnsiXml                  | A parsed representation of a non-Unicode XML document.                                                | Xml             | Xml             | Text             | TEXT             | XMLType          |
| Xml                      | A parsed representation of a non-Unicode XML document.                                                | Xml             | Xml             | Text             | TEXT             | XMLType          |
| AnsiJson                 | A parsed representation of a non-Unicode Json document.                                               | VarChar         | Json            | Json             | TEXT             | CLOB             |
| Json                     | A parsed representation of a non-Unicode Json document.                                               | NVarChar        | Json            | Json             | TEXT             | CLOB             |
| AnsiText                 | A variable-length stream of non-Unicode characters.                                                   | Text            | Text            | Text             | TEXT             | CLOB             |
| Text                     | A variable-length stream of Unicode characters.                                                       | NText           | Text            | Text             | TEXT             | NCLOB            |
| VarBinary                | A variable-length stream of binary data.                                                              | Binary          | Bytea           | Blob             | BLOB             | BLOB             |
| Binary                   | A variable-length stream of binary data.                                                              | VarBinary       | Bytea           | Blob             | BLOB             | BLOB             |
| Blob                     | A large Binary Large Object (BLOB) data type.                                                         | Image           | LargeObject     | LongBlob         | BLOB             | BLOB             |



## ColumnType

- `Default`-> A default column type, without any specific constraints or characteristics. This is the standard type for a column when no special behavior is needed.
- `PrimaryKey` -> Represents a column that is a primary key (or part of a primary key), uniquely identifying each record in the table. A primary key ensures the uniqueness of each row in a table.
- `AutoIncrement` -> The AutoIncrement attribute is used to specify that a column in a database table should automatically increment its value for each new record inserted. This is commonly used for primary key columns to generate unique identifiers automatically.
- `ConcurrencyCheck` -> Specifies that a column should be included in a concurrency check when updating a database record. This is used to ensure that the record has not been modified by another transaction before the update is applied.
- `RowVersion` -> Specifies that a column is used for optimistic concurrency control. It is a binary value that is automatically updated by the database whenever a row is modified. It is used to detect conflicts when multiple users are trying to modify the same row simultaneously.
- `HasDefaultValue` -> Indicates that the column in a database table has a default value. This is used when the database should automatically assign a default value if none is provided.
- `PrimaryKeyAutoIncrement` -> Represents a primary key column with an auto-increment feature, where the database automatically assigns a unique value when a new record is inserted. This is a combination of the PrimaryKey and AutoIncrement flags, for convenience.


# For additional documentation and examples  please refer to this [page]("xxx")

# Contributions 
Contributions to AltaSoft.DomainPrimitives are welcome! Whether you have suggestions or wish to contribute code, feel free to submit a pull request or open an issue.

# Contact
For support, questions, or additional information, please visit GitHub Issues.

# License
This project is licensed under [MIT](LICENSE.TXT). See the LICENSE file for details.


