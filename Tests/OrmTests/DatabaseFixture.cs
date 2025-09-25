using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AltaSoft.Storm.TestModels;
using Xunit;

namespace AltaSoft.Storm.Tests;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private const string TestBaseConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True;Encrypt=false;TrustServerCertificate=true";

    private readonly DatabaseHelper _databaseHelper;

    public string ConnectionString => _databaseHelper.ConnectionString;
    public List<User> Users { get; }
    public List<UserBulkCopy> UsersBulkCopy { get; }
    public List<CustomerProperty> CustomerProperties { get; }

    public DatabaseFixture()
    {
        _databaseHelper = new DatabaseHelper("storm-test-" + Guid.NewGuid(), TestBaseConnectionString);
        Users = _databaseHelper.Users;
        UsersBulkCopy = _databaseHelper.UsersBulkCopy;
        CustomerProperties = _databaseHelper.CustomerProperties;

        if (!StormManager.IsInitialized)
        {
            StormManager.Initialize(new MsSqlOrmProvider(), configuration =>
            {
                configuration.AddStormContext<TestStormContext>(dbConfig =>
                {
                    dbConfig.UseConnectionString(ConnectionString);
                    dbConfig.UseDefaultSchema("dbo");
                });
            });
        }
    }

    public Task InitializeAsync() => _databaseHelper.InitializeAsync();

    public Task DisposeAsync()
    {
        StormManager.SetLogger(null);

        return _databaseHelper.DisposeAsync();
    }
}
