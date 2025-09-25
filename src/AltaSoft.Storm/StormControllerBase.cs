using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.Crud;
using AltaSoft.Storm.Exceptions;
using AltaSoft.Storm.Extensions;
using AltaSoft.Storm.Interfaces;

namespace AltaSoft.Storm;

/// <summary>
/// Abstract base class for ORM (Object-Relational Mapping) controllers, providing common functionality for database operations.
/// </summary>
public abstract partial class StormControllerBase
{
    /// <summary>
    /// Storm context type associated with this controller.
    /// </summary>
    public abstract Type StormContext { get; }

    /// <summary>
    /// Gets the table's full name enclosed in quotes.
    /// </summary>
    public string QuotedObjectFullName { get; internal set; } = default!;

    /// <summary>
    /// Gets the schema name enclosed in quotes.
    /// </summary>
    public string QuotedSchemaName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the table name enclosed in quotes.
    /// </summary>
    public string QuotedObjectName { get; private set; }

    /// <summary>
    /// Gets the type of the database object.
    /// </summary>
    public DbObjectType ObjectType { get; private set; }

    /// <summary>
    /// Creates a data-bindable object from a database reader.
    /// </summary>
    /// <param name="dr">The database data reader.</param>
    /// <param name="partialLoadFlags">Flags indicating how the data should be partially loaded.</param>
    /// <param name="idx">The reference index for reading.</param>
    /// <returns>A new instance of a data-bindable object.</returns>
    public abstract IDataBindable Create(StormDbDataReader dr, uint partialLoadFlags, ref int idx);

    /// <summary>
    /// Reads a single scalar value from the provided DbDataReader at the current index and advances the index.
    /// </summary>
    /// <param name="dr">The DbDataReader to read from</param>
    /// <param name="propertyName">The name of the property to read</param>
    /// <param name="idx">The reference index for reading.</param>
    /// <returns>The scalar value read from the reader</returns>
    public abstract object? ReadSingleScalarValue(StormDbDataReader dr, string propertyName, ref int idx);

    /// <summary>
    /// Creates a detail row object based on the provided column definition, database data reader, and index.
    /// </summary>
    /// <param name="column">The column definition for the detail row.</param>
    /// <param name="dr">The database data reader containing the row data.</param>
    /// <param name="idx">The index of the current row in the data reader.</param>
    /// <returns>The created detail row object.</returns>
    public abstract object CreateDetailRow(StormColumnDef column, StormDbDataReader dr, ref int idx);

    /// <summary>
    /// Gets the column definitions for the ORM controller.
    /// </summary>
    public abstract StormColumnDef[] ColumnDefs { get; }

    ///// <summary>
    ///// Gets the key column definitions for the ORM controller.
    ///// </summary>
    //public abstract StormColumnDef[] KeyColumnDefs { get; }

    /// <summary>
    /// Gets the unique index/primary column definitions for the ORM controller.
    /// </summary>
    public abstract StormColumnDef[][] KeyColumnDefs { get; }

    /// <summary>
    /// Gets whether table has concurrency check columns.
    /// </summary>
    public abstract bool HasConcurrencyCheck { get; }

    /// <summary>
    /// Gets the partial load flags for all columns.
    /// </summary>
    public abstract uint PartialLoadFlagsAll { get; }

    /// <summary>
    /// Gets the flags for partial loading without details.
    /// </summary>
    public abstract uint PartialLoadFlagsAllWithoutDetails { get; }

    /// <summary>
    /// Retrieves the key value of an object from a DbDataReader.
    /// </summary>
    /// <param name="dr">The DbDataReader object to retrieve the value from.</param>
    /// <param name="idx">The index of the column to retrieve the value from. This parameter is passed by reference and will be updated to the index of the next column.</param>
    /// <returns>The value of the column at the specified index.</returns>
#pragma warning disable IDE1006

    public virtual object __ReadKeyValue(StormDbDataReader dr, ref int idx) => null!; // This null value is not used

    /// <summary>
    /// Returns the auto-increment column definition for a Storm entity.
    /// </summary>
    public abstract StormColumnDef? __GetAutoIncColumn();

#pragma warning restore IDE1006

    /// <summary>
    /// Sets the schema and table names for database operations, both quoted and unquoted.
    /// </summary>
    /// <param name="schemaName">The schema name.</param>
    public void SetSchemaName(string schemaName)
    {
        if (QuotedSchemaName.Length > 0) // Already set
            return;

        QuotedSchemaName = schemaName.QuoteSqlName();
        if (ObjectType is DbObjectType.VirtualView or DbObjectType.CustomSqlStatement)
            QuotedObjectFullName = QuotedObjectName.Replace("{%schema%}", QuotedSchemaName);
        else
            QuotedObjectFullName = QuotedSchemaName + '.' + QuotedObjectName;
    }

    /// <summary>
    /// Sets the schema and table names for database operations, both quoted and unquoted.
    /// </summary>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="objectName">The table name.</param>
    /// <param name="objectType">The database object type</param>
    protected StormControllerBase(string? schemaName, string objectName, DbObjectType objectType)
    {
        ObjectType = objectType;

        if (ObjectType is DbObjectType.VirtualView or DbObjectType.CustomSqlStatement)
            QuotedObjectName = '(' + objectName + ")";
        else
            QuotedObjectName = objectName.QuoteSqlName();

        if (schemaName is null)
        {
            if (ObjectType is DbObjectType.VirtualView or DbObjectType.CustomSqlStatement)
                QuotedObjectFullName = QuotedObjectName.Replace("{%schema%}", "dbo");
            return;
        }

        QuotedSchemaName = schemaName.QuoteSqlName();
        if (ObjectType is DbObjectType.VirtualView or DbObjectType.CustomSqlStatement)
            QuotedObjectFullName = QuotedObjectName.Replace("{%schema%}", QuotedSchemaName);
        else
            QuotedObjectFullName = QuotedSchemaName + '.' + QuotedObjectName;
    }

    #region Private methods

    /// <summary>
    /// Generates a list of WHERE SQL clauses based on the primary key columns of a given value.
    /// </summary>
    /// <param name="command">The database command object to which parameters will be added.</param>
    /// <param name="value">The value implementing IDataBindable from which primary key column values are extracted.</param>
    /// <param name="paramIndex">The starting index for parameterization in the SQL command.</param>
    /// <returns>A list of strings, each representing a WHERE clause for a primary key column in SQL format.</returns>
    private static (List<string> whereStatements, List<string> pkColumnNames, List<string> pkParamNames) GetPkInformation(IVirtualStormDbCommand command, IDataBindable value, ref int paramIndex)
    {
        var pid = paramIndex;

        var masterPkColumnNames = new List<string>(2);
        var masterPkParamNames = new List<string>(2);

        var whereStatements = value.__GetColumnValues().FilterAndSelectList(
            static x => (x.column.Flags & StormColumnFlags.Key) != StormColumnFlags.None,
                x =>
                {
                    var column = x.column;
                    var parameterName = command.AddDbParameter(pid++, column, column.GetValueForDbParameter(x.value, x.column.PropertySerializationType));

                    masterPkColumnNames.Add(column.ColumnName);
                    masterPkParamNames.Add(parameterName);

                    return column.ColumnName + '=' + parameterName;
                });

        paramIndex = pid;

        if (whereStatements.Count == 0)
            throw new StormException($"Primary key columns are not defined for type '{value.GetType().FullName}'.");

        return (whereStatements, masterPkColumnNames, masterPkParamNames);
    }

    /// <summary>
    /// Gets the partial loading data based on the provided flags and query parameters.
    /// </summary>
    /// <typeparam name="T">The type of data bindable.</typeparam>
    /// <param name="partialLoadFlags">The partial load flags.</param>
    /// <param name="queryParameters">The query parameters.</param>
    /// <returns>
    /// A tuple containing the updated partial load flags and a boolean indicating whether details should be loaded.
    /// </returns>
    internal (uint partialLoadFlags, bool shouldLoadDetails) GetPartialLoadingData<T>(uint partialLoadFlags, SelectQueryParameters<T> queryParameters) where T : IDataBindable
    {
        var allFlags = PartialLoadFlagsAll;
        if (allFlags == 0) // Not detail tables at all
            return (0, false);

        if (queryParameters.LoadDetailTables)
            return (partialLoadFlags, ColumnDefs.ShouldLoadDetails(partialLoadFlags));

        var allExceptDetailsFlag = PartialLoadFlagsAllWithoutDetails;
        partialLoadFlags &= allExceptDetailsFlag; // Remove any extra flags
        return (partialLoadFlags, false);
    }

    /// <summary>
    /// Array containing all StormTableHints except for None.
    /// </summary>
    private static readonly StormTableHints[] s_allHints = Enum.GetValues<StormTableHints>().Where(hint => hint != StormTableHints.None).ToArray();

    /// <summary>
    /// Appends table hints to a StringBuilder based on the provided StormTableHints enum value.
    /// </summary>
    /// <param name="hints">The StormTableHints enum value indicating the hints to be appended.</param>
    /// <param name="sb">The StringBuilder to which the hints will be appended.</param>
    private static void AppendTableHints(StormTableHints hints, StringBuilder sb)
    {
        if (hints == StormTableHints.None)
            return;

        var isFirstHint = true;

        sb.Append(" WITH (");
        foreach (var hint in s_allHints)
        {
            if ((hints & hint) != hint)
                continue;

            if (!isFirstHint)
                sb.Append(", ");

            sb.Append(GetHintString(hint));
            isFirstHint = false;
        }
        sb.Append(')');
    }

    /// <summary>
    /// Returns the corresponding SQL hint string for the given StormTableHints enum value.
    /// </summary>
    /// <param name="hint">The StormTableHints enum value for which to get the SQL hint string.</param>
    /// <returns>
    /// The SQL hint string corresponding to the provided StormTableHints enum value.
    /// </returns>
    private static string GetHintString(StormTableHints hint)
    {
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return hint switch
        {
            StormTableHints.NoLock => "NOLOCK",
            StormTableHints.ReadUncommitted => "READUNCOMMITTED",
            StormTableHints.UpdLock => "UPDLOCK",
            StormTableHints.HoldLock => "HOLDLOCK",
            StormTableHints.ReadCommitted => "READCOMMITTED",
            StormTableHints.RepeatableRead => "REPEATABLEREAD",
            StormTableHints.Serializable => "SERIALIZABLE",
            StormTableHints.TabLock => "TABLOCK",
            StormTableHints.TabLockX => "TABLOCKX",
            StormTableHints.PagLock => "PAGLOCK",
            StormTableHints.RowLock => "ROWLOCK",
            StormTableHints.XLock => "XLOCK",
            StormTableHints.NoWait => "NOWAIT",
            StormTableHints.ForceSeek => "FORCESEEK",
            StormTableHints.ForceScan => "FORCESCAN",
            StormTableHints.IgnoreConstraints => "IGNORE_CONSTRAINTS",
            StormTableHints.IgnoreTriggers => "IGNORE_TRIGGERS",
            StormTableHints.KeepIdentity => "KEEPIDENTITY",
            StormTableHints.KeepDefaults => "KEEPDEFAULTS",
            StormTableHints.Ties => "TIES",
            StormTableHints.NoExpand => "NOEXPAND",
            StormTableHints.ReadPast => "READPAST",
            _ => throw new ArgumentOutOfRangeException(nameof(hint), hint, null)
        };
    }

    #endregion Private methods
}
