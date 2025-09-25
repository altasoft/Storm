using System.Linq;

namespace AltaSoft.Storm;

/// <summary>
/// StormConfiguration class for configuring StormContext with a connection string.
/// </summary>
public sealed class StormContextConfiguration<TStormContext> where TStormContext : StormContext
{
    internal bool IsSchemaAssigned { get; private set; }

    /// <summary>
    /// This method sets connection string for a StormContext.
    /// </summary>
    public void UseConnectionString(string connectionString)
    {
        StormContext.SetDefaultConnectionString(typeof(TStormContext), connectionString);
    }

    /// <summary>
    /// This method sets default schema for a StormContext.
    /// </summary>
    public void UseDefaultSchema(string schema)
    {
        StormContext.SetDefaultSchema(typeof(TStormContext), schema);
        IsSchemaAssigned = true;

        foreach (var ctrl in StormControllerCache.GetAllControllers()
            .Where(x => x.StormContext == typeof(TStormContext)))
        {
            ctrl.SetSchemaName(schema);
        }
    }
}
