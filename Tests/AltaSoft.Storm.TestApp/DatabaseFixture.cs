using AltaSoft.Storm.TestModels;

namespace AltaSoft.Storm.TestApp;

public sealed class DatabaseFixture
{
    private const string TestBaseConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True;Encrypt=false;TrustServerCertificate=true";

    private readonly DatabaseHelper _databaseHelper;

    public string ConnectionString => _databaseHelper.ConnectionString;
    public List<User> Users { get; }
    public List<CustomerProperty> CustomerProperties { get; }

    public DatabaseFixture()
    {
        if (!StormManager.IsInitialized)
        {
            StormManager.Initialize(new MsSqlOrmProvider(), configuration =>
                {
                    configuration.AddStormContext<TestStormContext>(dbConfiguration =>
                    {
                        dbConfiguration.UseConnectionString(ConnectionString);
                        dbConfiguration.UseDefaultSchema("dbo");
                    });

                });
        }

        _databaseHelper = new DatabaseHelper("storm-test-" + Guid.NewGuid(), TestBaseConnectionString);
        Users = _databaseHelper.Users;
        CustomerProperties = _databaseHelper.CustomerProperties;
    }

    public Task InitializeAsync() => _databaseHelper.InitializeAsync();

    public Task DisposeAsync()
    {
        StormManager.SetLogger(null);

        return _databaseHelper.DisposeAsync();
    }
}
