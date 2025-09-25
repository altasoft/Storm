using System;

namespace AltaSoft.Storm;

/// <summary>
/// Provides configuration methods for registering and setting up Storm contexts.
/// </summary>
public sealed class StormConfiguration
{
    /// <summary>
    /// Adds a new Storm context configuration.
    /// </summary>
    /// <typeparam name="TStormContext">The type of the <see cref="StormContext"/> to configure.</typeparam>
    /// <param name="dbConfiguration">
    /// An action to configure the Storm context using a <see cref="StormContextConfiguration{TStormContext}"/> object.
    /// </param>
    /// <remarks>
    /// If no schema is assigned during configuration, the default schema "dbo" will be used.
    /// </remarks>
    public void AddStormContext<TStormContext>(Action<StormContextConfiguration<TStormContext>> dbConfiguration)
        where TStormContext : StormContext
    {
        var configuration = new StormContextConfiguration<TStormContext>();
        dbConfiguration(configuration);

        // Check that schema is assigned
        if (!configuration.IsSchemaAssigned)
            configuration.UseDefaultSchema("dbo");
    }
}
