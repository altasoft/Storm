using System;

namespace AltaSoft.Storm.Attributes;

/// <summary>
/// Attribute used to designate a class as a controller for a specific table in a database schema.
/// This attribute is used to associate a class with a database table, specifying the schema name, table name, and the type it controls.
/// </summary>
/// <remarks>
/// This attribute can be applied to classes to define their role as controllers in the Storm ORM system,
/// allowing for mapping between database tables and business logic.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class StormControllerAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the database schema associated with the controller.
    /// If null, a default schema may be used.
    /// </summary>
    public string? SchemaName { get; }

    /// <summary>
    /// Gets the name of the table in the database that this controller manages.
    /// </summary>
    public string ObjectName { get; }

    /// <summary>
    /// Gets or sets the type that this controller is managing.
    /// This property typically refers to an entity class that maps to the specified database table.
    /// </summary>
    public Type ControllerOf { get; }

    /// <summary>
    /// Gets the variant value.
    /// </summary>
    public int Variant { get; }

    /// <summary>
    /// Gets the type of the database object.
    /// </summary>
    public DbObjectType ObjectType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StormControllerAttribute"/> class.
    /// </summary>
    /// <param name="schemaName">The name of the database schema.</param>
    /// <param name="objectName">The name of the table in the database.</param>
    /// <param name="controllerOf">The type that this controller is managing.</param>
    /// <param name="variant">The variant of the controller</param>
    /// <param name="objectType">The database object type</param>
    public StormControllerAttribute(string? schemaName, string objectName, Type controllerOf, int variant, DbObjectType objectType)
    {
        SchemaName = schemaName;
        ObjectName = objectName;
        ControllerOf = controllerOf;
        Variant = variant;
        ObjectType = objectType;
    }
}
