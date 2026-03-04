# Initialization and context setup

## App Startup (program.cs)

Register all Storm contexts at app startup using `StormManager.Initialize`. Connection strings are configured once here, not when creating context instances:

```csharp
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

Register multiple contexts if needed:

```csharp
configuration.AddStormContext<MyAppContext>(dbConfig =>
{
    dbConfig.UseConnectionString("your-connection-string");
    dbConfig.UseDefaultSchema("dbo");
});

configuration.AddStormContext<PaymentsContext>(dbConfig =>
{
    dbConfig.UseConnectionString("payments-connection-string");
    dbConfig.UseDefaultSchema("dbo");
});
```

If the project uses logging, set the logger once:

```csharp
StormManager.SetLogger(logger);
```

## Context Class Setup

Declare your context class using minimal syntax:

```csharp
public sealed partial class MyAppContext : StormContext;
```

Optionally, add a constructor for testing or override scenarios:

```csharp
public sealed partial class MyAppContext : StormContext
{
    public MyAppContext(string connectionString) : base(connectionString)
    {
    }
}
```

## Creating Context Instances

Create context instances with the automatic connection string retrieval from program.cs registration:

```csharp
await using var context = new MyAppContext();
```

Pass connection string parameter only for testing or override scenarios.
```

Best practice: Use one StormContext per project, database, or clear domain grouping. Create a new context instance per operation or per scope.
