using System;

namespace AltaSoft.Storm.Attributes;

/// <summary>
/// Attribute used to bind a class to a database table.
/// <example>
/// Usage example:
/// <code>
/// [StormDbObject(SchemaName = "dbo", ObjectName = "Customers")]
/// public class Customer
/// {
///     // Property definitions
/// }
/// </code>
/// </example>
/// </summary>
/// <remarks>
/// This attribute can be applied to a class, record, or struct to specify that it should be part of Storm ORM.
/// It provides the ORM with the necessary information to map the class to the appropriate database table.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class StormDbObjectAttribute<TContext> : Attribute where TContext : StormContext
{
    /// <summary>
    /// The name of the database schema. If left null, the default schema is used.
    /// This allows mapping the class to a specific schema in the database.
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// The name of the table. If left null, the pluralized class name is used.
    /// This property defines the table name in the database that the class will be mapped to.
    /// </summary>
    public string? ObjectName { get; set; }

    /// <summary>
    /// The type of the database object, default value is Table.
    /// </summary>
    public DbObjectType ObjectType { get; set; } = DbObjectType.Table;

    /// <summary>
    /// The display name of an object. Can be null.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// The mode for database update.
    /// The default value is <see cref="Attributes.UpdateMode.ChangeTracking"/>.
    /// This property determines how changes to the object are tracked and updated in the database.
    /// </summary>
    public UpdateMode UpdateMode { get; set; } = UpdateMode.ChangeTracking;

    /// <summary>
    /// The SQL statement of the virtual view. If left null, then SQL statement should be provided at runtime.
    /// </summary>
    public string? VirtualViewSql { get; set; }

    /// <summary>
    /// Indicates whether bulk copy operations should be generated for this object.
    /// </summary>
    public bool BulkInsert { get; set; }
}
