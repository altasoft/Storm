using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using AltaSoft.Storm.Generator.Common;

namespace AltaSoft.Storm.Models;

/// <summary>
/// Represents a collection of <see cref="DbEntityDef"/> objects.
/// This collection provides methods to find and manipulate database entity definitions.
/// </summary>
internal class DbEntityDefList : ObservableCollection<DbEntityDef>
{
    /// <summary>
    /// Retrieves the DbEntityDef object based on the specified DupDbObjectType.
    /// </summary>
    public DbEntityDef GetDbEntityByType(DupDbObjectType objectType) => this.First(x => x.Type == objectType);

    /// <summary>
    /// Finds a <see cref="DbObjectDef"/> within the collection based on the specified table name and optional schema name.
    /// </summary>
    /// <param name="objectType">The type of DbObject to search for.</param>
    /// <param name="name">The name of the table to find.</param>
    /// <param name="schemaName">The schema name associated with the table. This parameter is optional.</param>
    /// <returns>
    /// The <see cref="DbObjectDef"/> if found; otherwise, null.
    /// </returns>
    public DbObjectDef? FindDbObject(DupDbObjectType objectType, string name, string? schemaName)
    {
        return GetDbEntityByType(objectType).DbObjects.FirstOrDefault(x => x.ObjectName == name && (schemaName is null || schemaName == x.SchemaName));
    }

    /// <summary>
    /// Finds a <see cref="DbObjectDef"/> within the collection based on a specified search predicate.
    /// </summary>
    /// <param name="objectType">The type of DbObject to search for.</param>
    /// <param name="predicate">The search predicate to use for finding the <see cref="DbObjectDef"/>.</param>
    /// <returns>
    /// The <see cref="DbObjectDef"/> if found; otherwise, null.
    /// </returns>
    public DbObjectDef? FindDbObject(DupDbObjectType objectType, Func<DbObjectDef, bool> predicate)
    {
        return GetDbEntityByType(objectType).DbObjects.FirstOrDefault(predicate);
    }

    /// <summary>
    /// Refreshes the collection by triggering a 'Replace' collection changed event.
    /// </summary>
    public void Refresh()
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace));
    }
}
