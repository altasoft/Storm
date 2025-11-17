using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.TestModels;
using Microsoft.Data.SqlClient;

namespace AltaSoft.Storm.Tests;

public sealed class DatabaseHelper
{
    private readonly string _baseConnectionString;

    private readonly string _dbName;

    public string ConnectionString { get; }

    public List<User> Users { get; }
    public List<CustomerProperty> CustomerProperties { get; }
    public List<UserBulkCopy> UsersBulkCopy { get; set; }

    public DatabaseHelper(string dbName, string baseConnectionString)
    {
        _dbName = dbName;
        _baseConnectionString = baseConnectionString;

        var builder = new SqlConnectionStringBuilder(_baseConnectionString)
        {
            InitialCatalog = _dbName
        };

        ConnectionString = builder.ToString();

        Users = CreateUserList();
        CustomerProperties = CreateCustomerProperties();
        UsersBulkCopy = CreateBulkCopyUsersList();
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqlConnection(_baseConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await connection.CreateDatabaseAsync(_dbName, true).ConfigureAwait(false);
        await connection.UseDatabaseAsync(_dbName).ConfigureAwait(false);
        await connection.CreateSchemaAsync("test", true).ConfigureAwait(false);

        await connection.CreateTableAsync<User>(true).ConfigureAwait(false);
        await connection.CreateTableAsync<SysAdmin>(true).ConfigureAwait(false);

        await connection.CreateTableAsync<ClassWithTimestamp>(true).ConfigureAwait(false);

        await connection.CreateTableAsync<CustomerProperty>(true).ConfigureAwait(false);

        await connection.CreateTableAsync<UserBulkCopy>(true).ConfigureAwait(false);
        await connection.CreateTableAsync<CompressedData>(true).ConfigureAwait(false);

        await connection.CreateTableAsync<Account>(true).ConfigureAwait(false);

        await using var context = new TestStormContext(ConnectionString);

        // Create list of users
        await context.InsertIntoUsersTable().Values(Users).GoAsync();

        // Create list of customer properties
        await context.InsertIntoCustomerProperties().Values(CustomerProperties).GoAsync();

        await connection.ExecuteSqlStatementAsync(
            """
            CREATE VIEW [dbo].[UsersView]
            AS
            	SELECT *
            	FROM dbo.Users
            """);

        await connection.ExecuteSqlStatementAsync(
            """
            CREATE FUNCTION [dbo].[users_func] (@userId int)
            RETURNS TABLE AS RETURN
            (
            	SELECT *
            	FROM dbo.Users
            );
            """);

        await connection.ExecuteSqlStatementAsync(
            """
            CREATE PROCEDURE [dbo].[users_proc] (@userId int, @io int OUTPUT)
            AS
            BEGIN
                SET @io = 77;

            	SELECT *
            	FROM dbo.Users
            END;
            """);

        await connection.ExecuteSqlStatementAsync(
            """
            CREATE FUNCTION [dbo].[ScalarFunc2] (@userId int, @branch_id int)
            RETURNS int
            AS
            BEGIN
            	RETURN 77;
            END;
            """);

        await connection.ExecuteSqlStatementAsync(
            """
            CREATE PROCEDURE [test].[InputOutputProc] (@user_id int, @result_id int OUT, @io int OUT)
            AS
            BEGIN
            	SET @result_id = @user_id;
            	SET @io = 77;
            	RETURN 1;
            END;
            """);
    }

    public async Task DisposeAsync()
    {
        await using var connection = new SqlConnection(_baseConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);

        await ExecuteDbCommandAsync(connection, $"EXEC msdb.dbo.sp_delete_database_backuphistory @database_name = N'{_dbName}'").ConfigureAwait(false);
        await connection.UseDatabaseAsync("master").ConfigureAwait(false);
        await connection.DropDatabaseAsync(_dbName, true).ConfigureAwait(false);
    }

    private static async Task ExecuteDbCommandAsync(SqlConnection connection, string commandText)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = commandText;
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
    private static List<UserBulkCopy> CreateBulkCopyUsersList()
    {
        var userList = new List<UserBulkCopy>();
        for (var i = 1; i <= 1_000; i++)
        {
            userList.Add(NewUserBulkCopy(i));
        }

        return userList;
    }
    public static List<User> CreateUserList()
    {
        var userList = new List<User>();
        for (var i = 1; i <= 10; i++)
        {
            userList.Add(DatabaseHelper.NewUser(i));
        }

        return userList;
    }

    private static List<CustomerProperty> CreateCustomerProperties()
    {
        var customerPropsList = new List<CustomerProperty>();
        for (var i = 1; i <= 5; i++)
        {
            customerPropsList.Add(new CustomerProperty
            {
                Id = i,
                Name = $"A:Name {i}",
                Value = $"Value {i}"
            });

            customerPropsList.Add(new CustomerProperty
            {
                Id = i,
                Name = $"B:Name {i}",
                Value = $"Value {i}"
            });
        }

        return customerPropsList;
    }
    public static UserBulkCopy NewUserBulkCopy(int id)
    {
        return new UserBulkCopy
        {
            UserId = id,
            BranchId = 7,
            AutoInc = id,
            Roles = [4, 5, 6],
            LoginName = "Demo",
            FullName = "Demo user with unicode chars (˿)",
            TwoValues = new TwoValues { I1 = Random.Shared.Next(), I2 = Random.Shared.Next() },
            DatePair = new DatePair { Date1 = DateOnly.FromDateTime(DateTime.Now.Date), Date2 = null },
            //Phones = new List<string> { "577418095", "12345678", "+12345678" },
            CustomerId = Random.Shared.Next(),
            CustomerId2 = null,
            CurrencyId = "USD"
        };
    }

    public static User NewUser(int id)
    {
        return new User
        {
            UserId = id,
            BranchId = 7,
            AutoInc = id,
            Roles = [4, 5, 6],
            LoginName = "Demo",
            FullName = "Demo user with unicode chars (˿)",
            TwoValues = new TwoValues { I1 = Random.Shared.Next(), I2 = Random.Shared.Next() },
            DatePair = new DatePair { Date1 = DateOnly.FromDateTime(DateTime.Now.Date), Date2 = null },
            //Phones = new List<string> { "577418095", "12345678", "+12345678" },
            CustomerId = Random.Shared.Next(),
            CustomerId2 = Random.Shared.Next(),
            CurrencyId = "USD",
            Dates =
            [
                new DatePair
                {
                    Date1 = DateOnly.FromDateTime(DateTime.Today),
                    Date2 = DateOnly.FromDateTime(DateTime.Today.AddDays(Random.Shared.Next(100)))
                },
                new DatePair { Date1 = DateOnly.FromDateTime(DateTime.Today.AddDays(2)), Date2 = null }
            ],
            ListOfStrings = new List<string> { "Demo", "User", "With", "Unicode", "Chars" },
            ListOfIntegers = new List<int> { 1, 3, 7, 9, -20 },
            Cars = new List<Car>
            {
                new() { CarId = Guid.NewGuid(), Model = "Audi", Year = 2020, Color = RgbColor.Red },
                new() { CarId = Guid.NewGuid(), Model = "Lexus", Year = 2021, Color = RgbColor.Green },
                new() { CarId = Guid.NewGuid(), Model = "Toyota", Year = 2019, Color = RgbColor.Blue }
            }
        };
    }

    public static SysAdmin NewSysAdmin(int id)
    {
        return new SysAdmin()
        {
            Sid = 111,
            UserId = id,
            BranchId = 7,
            AutoInc = id,
            Roles = [4, 5, 6],
            LoginName = "Demo",
            FullName = "Demo user with unicode chars (˿)",
            TwoValues = new TwoValues { I1 = Random.Shared.Next(), I2 = Random.Shared.Next() },
            DatePair = new DatePair { Date1 = DateOnly.FromDateTime(DateTime.Now.Date), Date2 = null },
            //Phones = new List<string> { "577418095", "12345678", "+12345678" },
            CustomerId = Random.Shared.Next(),
            CustomerId2 = Random.Shared.Next(),
            CurrencyId = "USD",
            Dates =
            [
                new DatePair
                {
                    Date1 = DateOnly.FromDateTime(DateTime.Today),
                    Date2 = DateOnly.FromDateTime(DateTime.Today.AddDays(Random.Shared.Next(100)))
                },
                new DatePair { Date1 = DateOnly.FromDateTime(DateTime.Today.AddDays(2)), Date2 = null }
            ],
            ListOfStrings = new List<string> { "Demo", "User", "With", "Unicode", "Chars" },
            ListOfIntegers = new List<int> { 1, 3, 7, 9, -20 },
            Cars = new List<Car>
            {
                new() { CarId = Guid.NewGuid(), Model = "Audi", Year = 2020, Color = RgbColor.Red },
                new() { CarId = Guid.NewGuid(), Model = "Lexus", Year = 2021, Color = RgbColor.Green },
                new() { CarId = Guid.NewGuid(), Model = "Toyota", Year = 2019, Color = RgbColor.Blue }
            }
        };
    }
}
