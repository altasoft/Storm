using System;
using System.Collections.Generic;
using AltaSoft.Storm.Interfaces;

// ReSharper disable UnusedMember.Global

namespace AltaSoft.Storm.Crud;

/// <summary>
/// Factory class for creating CRUD operations in Storm.
/// </summary>
public static class StormCrudFactory
{
    /// <summary>
    /// Creates an instance of <see cref="IExecuteProc{TResult}"/> for executing a stored procedure.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the stored procedure.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="callParameters">The list of call parameters for the stored procedure.</param>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="objectName">The name of the stored procedure.</param>
    /// <param name="resultReader">The function to read the result of the stored procedure.</param>
    /// <returns>An instance of <see cref="IExecuteProc{TResult}"/>.</returns>
    public static IExecuteProc<TResult> ExecuteProc<TResult>(StormContext context,
        List<StormCallParameter> callParameters, string? schemaName, string objectName,
        Func<int, StormDbParameterCollection, Exception?, TResult> resultReader)
        where TResult : StormProcedureResult
    {
        return new ExecuteProc<TResult>(context, callParameters, schemaName, objectName, resultReader);
    }

    /// <summary>
    /// Creates an instance of <see cref="IExecuteScalarFunc{TResult}"/> for executing a scalar function.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the scalar function.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="callParameters">The list of call parameters for the scalar function.</param>
    /// <param name="schemaName">The name of the schema.</param>
    /// <param name="objectName">The name of the scalar function.</param>
    /// <param name="resultReader">The function to read the result of the scalar function.</param>
    /// <returns>An instance of <see cref="IExecuteScalarFunc{TResult}"/>.</returns>
    public static IExecuteScalarFunc<TResult> ExecuteScalarFunc<TResult>(StormContext context,
        List<StormCallParameter> callParameters,
        string? schemaName,
        string objectName,
        Func<StormDbDataReader, TResult> resultReader)
    {
        return new ExecuteScalarFunc<TResult>(context, callParameters, schemaName, objectName, resultReader);
    }

    #region Select

    /// <summary>
    /// Creates an instance of <see cref="ISelectFrom{T, TOrderBy, TPartialLoadFlags}"/> for selecting data from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to select.</typeparam>
    /// <typeparam name="TOrderBy">The type of the order by enumeration.</typeparam>
    /// <typeparam name="TPartialLoadFlags">The type of the partial load flags enumeration.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the select operation.</param>
    /// <param name="keyValues">The key values to identify the record.</param>
    /// <param name="keyId">ID of the unique index/primary key in KeyColumnDefs array.</param>
    /// <returns>An instance of <see cref="ISelectFrom{T, TOrderBy, TPartialLoadFlags}"/>.</returns>
    public static ISelectFrom<T, TOrderBy, TPartialLoadFlags> SelectFrom<T, TOrderBy, TPartialLoadFlags>(StormContext context, int variant, object[] keyValues, int keyId)
        where T : IDataBindable
        where TOrderBy : struct, Enum
        where TPartialLoadFlags : struct, Enum
    {
        return new SelectFrom<T, TOrderBy, TPartialLoadFlags>(context, variant, keyValues, keyId);
    }

    /// <summary>
    /// Creates an instance of <see cref="ISelectFrom{T, TOrderBy, TPartialLoadFlags}"/> for selecting data from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to select.</typeparam>
    /// <typeparam name="TOrderBy">The type of the order by enumeration.</typeparam>
    /// <typeparam name="TPartialLoadFlags">The type of the partial load flags enumeration.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the select operation.</param>
    /// <param name="keyValues">The key values to identify the record.</param>
    /// <param name="keyId">ID of the unique index/primary key in KeyColumnDefs array.</param>
    /// <param name="customSqlStatement">The custom SQL select statement.</param>
    /// <param name="callParameters">The list of call parameters for the select operation.</param>
    /// <returns>An instance of <see cref="ISelectFrom{T, TOrderBy, TPartialLoadFlags}"/>.</returns>
    public static ISelectFrom<T, TOrderBy, TPartialLoadFlags> SelectFrom<T, TOrderBy, TPartialLoadFlags>(StormContext context, int variant, object[] keyValues, int keyId, string customSqlStatement, List<StormCallParameter>? callParameters)
        where T : IDataBindable
        where TOrderBy : struct, Enum
        where TPartialLoadFlags : struct, Enum
    {
        return new SelectFromCustomSqlStatement<T, TOrderBy, TPartialLoadFlags>(context, variant, keyValues, keyId, customSqlStatement, callParameters);
    }

    /// <summary>
    /// Creates an instance of <see cref="ISelectFrom{T, TOrderBy, TPartialLoadFlags}"/> for selecting data from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to select.</typeparam>
    /// <typeparam name="TOrderBy">The type of the order by enumeration.</typeparam>
    /// <typeparam name="TPartialLoadFlags">The type of the partial load flags enumeration.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the select operation.</param>
    /// <returns>An instance of <see cref="ISelectFrom{T, TOrderBy, TPartialLoadFlags}"/>.</returns>
    public static ISelectFrom<T, TOrderBy, TPartialLoadFlags> SelectFrom<T, TOrderBy, TPartialLoadFlags>(StormContext context, int variant)
        where T : IDataBindable
        where TOrderBy : struct, Enum
        where TPartialLoadFlags : struct, Enum
    {
        return new SelectFrom<T, TOrderBy, TPartialLoadFlags>(context, variant);
    }

    /// <summary>
    /// Creates an instance of <see cref="ISelectFrom{T, TOrderBy, TPartialLoadFlags}"/> for selecting data from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to select.</typeparam>
    /// <typeparam name="TOrderBy">The type of the order by enumeration.</typeparam>
    /// <typeparam name="TPartialLoadFlags">The type of the partial load flags enumeration.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the select operation.</param>
    /// <param name="customSqlStatement">The custom SQL select statement</param>
    /// <param name="callParameters">The list of call parameters for the select operation.</param>
    /// <returns>An instance of <see cref="ISelectFrom{T, TOrderBy, TPartialLoadFlags}"/>.</returns>
    public static ISelectFrom<T, TOrderBy, TPartialLoadFlags> SelectFrom<T, TOrderBy, TPartialLoadFlags>(StormContext context, int variant, string customSqlStatement, List<StormCallParameter>? callParameters)
        where T : IDataBindable
        where TOrderBy : struct, Enum
        where TPartialLoadFlags : struct, Enum
    {
        return new SelectFromCustomSqlStatement<T, TOrderBy, TPartialLoadFlags>(context, variant, customSqlStatement, callParameters);
    }

    /// <summary>
    /// Creates an instance of <see cref="ISelectFrom{T, TOrderBy, TPartialLoadFlags}"/> for selecting data from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to select.</typeparam>
    /// <typeparam name="TOrderBy">The type of the order by enumeration.</typeparam>
    /// <typeparam name="TPartialLoadFlags">The type of the partial load flags enumeration.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the select operation.</param>
    /// <param name="callParameters">The list of call parameters for the select operation.</param>
    /// <returns>An instance of <see cref="ISelectFrom{T, TOrderBy, TPartialLoadFlags}"/>.</returns>
    public static ISelectFrom<T, TOrderBy, TPartialLoadFlags> SelectFrom<T, TOrderBy, TPartialLoadFlags>(StormContext context, int variant, List<StormCallParameter> callParameters)
        where T : IDataBindable
        where TOrderBy : struct, Enum
        where TPartialLoadFlags : struct, Enum
    {
        return new SelectFrom<T, TOrderBy, TPartialLoadFlags>(context, variant, callParameters);
    }

    /// <summary>
    /// Creates an instance of <see cref="ISelectFromSingle{T, TOrderBy, TPartialLoadFlags}"/> for selecting a single record from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to select.</typeparam>
    /// <typeparam name="TOrderBy">The type of the order by enumeration.</typeparam>
    /// <typeparam name="TPartialLoadFlags">The type of the partial load flags enumeration.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the select operation.</param>
    /// <param name="keyValues">The key values to identify the record.</param>
    /// <param name="keyId">ID of the unique index/primary key in KeyColumnDefs array.</param>
    /// <param name="virtualViewSql">The SQL statement for virtual view</param>
    /// <param name="callParameters">The list of call parameters for the select operation.</param>
    /// <returns>An instance of <see cref="ISelectFromSingle{T, TOrderBy, TPartialLoadFlags}"/>.</returns>
    public static ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> SelectFromSingle<T, TOrderBy, TPartialLoadFlags>(StormContext context, int variant, object[] keyValues, int keyId, string virtualViewSql, List<StormCallParameter>? callParameters)
        where T : IDataBindable
        where TOrderBy : struct, Enum
        where TPartialLoadFlags : struct, Enum
    {
        return new SelectFromSingleCustomSqlStatement<T, TOrderBy, TPartialLoadFlags>(context, variant, keyValues, keyId, virtualViewSql, callParameters);
    }

    /// <summary>
    /// Creates an instance of <see cref="ISelectFromSingle{T, TOrderBy, TPartialLoadFlags}"/> for selecting a single record from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to select.</typeparam>
    /// <typeparam name="TOrderBy">The type of the order by enumeration.</typeparam>
    /// <typeparam name="TPartialLoadFlags">The type of the partial load flags enumeration.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the select operation.</param>
    /// <param name="keyValues">The key values to identify the record.</param>
    /// <param name="keyId">ID of the unique index/primary key in KeyColumnDefs array.</param>
    /// <returns>An instance of <see cref="ISelectFromSingle{T, TOrderBy, TPartialLoadFlags}"/>.</returns>
    public static ISelectFromSingle<T, TOrderBy, TPartialLoadFlags> SelectFromSingle<T, TOrderBy, TPartialLoadFlags>(StormContext context, int variant, object[] keyValues, int keyId)
        where T : IDataBindable
        where TOrderBy : struct, Enum
        where TPartialLoadFlags : struct, Enum
    {
        return new SelectFromSingle<T, TOrderBy, TPartialLoadFlags>(context, variant, keyValues, keyId);
    }

    /// <summary>
    /// Creates an instance of <see cref="IExecuteFrom{T, TOutput}"/> for executing a select operation with output parameters.
    /// </summary>
    /// <typeparam name="T">The type of the data to select.</typeparam>
    /// <typeparam name="TOutput">The type of the output result.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the select operation.</param>
    /// <param name="callParameters">The list of call parameters for the select operation.</param>
    /// <param name="outputWriter">The action to write the output result.</param>
    /// <returns>An instance of <see cref="IExecuteFrom{T, TOutput}"/>.</returns>
    public static IExecuteFrom<T, TOutput> ExecuteFrom<T, TOutput>(StormContext context, int variant, List<StormCallParameter> callParameters, Action<int, StormDbParameterCollection, TOutput> outputWriter)
        where T : IDataBindable
        where TOutput : StormProcedureResult, new()
    {
        return new ExecuteFrom<T, TOutput>(context, variant, callParameters, outputWriter);
    }

    #endregion Select

    #region Delete

    /// <summary>
    /// Creates an instance of <see cref="IDeleteFrom{T}"/> for deleting data from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to delete.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the delete operation.</param>
    /// <returns>An instance of <see cref="IDeleteFrom{T}"/>.</returns>
    public static IDeleteFrom<T> DeleteFrom<T>(StormContext context, int variant) where T : IDataBindable
    {
        return new DeleteFrom<T>(context, variant);
    }

    /// <summary>
    /// Creates an instance of <see cref="IDeleteFrom{T}"/> for deleting data from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to delete.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the delete operation.</param>
    /// <param name="customQuotedObjectFullName">
    /// Fully-qualified and properly quoted table name.
    /// Use this when targeting a specific schema or custom-mapped table.
    /// </param>
    /// <returns>An instance of <see cref="IDeleteFrom{T}"/>.</returns>
    public static IDeleteFrom<T> DeleteFrom<T>(StormContext context, int variant, string customQuotedObjectFullName) where T : IDataBindable
    {
        return new DeleteFrom<T>(context, variant, customQuotedObjectFullName);
    }

    /// <summary>
    /// Creates an instance of <see cref="IDeleteFromSingle{T}"/> for deleting a single record from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to delete.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the delete operation.</param>
    /// <param name="keyValues">The key values to identify the record.</param>
    /// <param name="keyId">ID of the unique index/primary key in KeyColumnDefs array.</param>
    /// <returns>An instance of <see cref="IDeleteFromSingle{T}"/>.</returns>
    public static IDeleteFromSingle<T> DeleteFromSingle<T>(StormContext context, int variant, object[] keyValues, int keyId) where T : IDataBindable
    {
        return new DeleteFromSingle<T>(context, variant, keyValues, keyId);
    }

    /// <summary>
    /// Creates an instance of <see cref="IDeleteFromSingle{T}"/> for deleting a single record from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to delete.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the delete operation.</param>
    /// <param name="keyValues">The key values to identify the record.</param>
    /// <param name="keyId">ID of the unique index/primary key in KeyColumnDefs array.</param>
    /// <param name="customQuotedObjectFullName">
    /// Fully-qualified and properly quoted table name.
    /// Use this when targeting a specific schema or custom-mapped table.
    /// </param>
    /// <returns>An instance of <see cref="IDeleteFromSingle{T}"/>.</returns>
    public static IDeleteFromSingle<T> DeleteFromSingle<T>(StormContext context, int variant, object[] keyValues, int keyId, string customQuotedObjectFullName) where T : IDataBindable
    {
        return new DeleteFromSingle<T>(context, variant, keyValues, keyId, customQuotedObjectFullName);
    }

    /// <summary>
    /// Creates an instance of <see cref="IDeleteFromSingle{T}"/> for deleting a single record from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to delete.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the delete operation.</param>
    /// <param name="value">The value to delete.</param>
    /// <returns>An instance of <see cref="IDeleteFromSingle{T}"/>.</returns>
    public static IDeleteFromSingle<T> DeleteFromSingle<T>(StormContext context, int variant, T value) where T : IDataBindable
    {
        return new DeleteFromSingle<T>(context, variant, value);
    }

    /// <summary>
    /// Creates an instance of <see cref="IDeleteFromSingle{T}"/> for deleting a single record from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to delete.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the delete operation.</param>
    /// <param name="value">The value to delete.</param>
    /// <param name="customQuotedObjectFullName">
    /// Fully-qualified and properly quoted table name.
    /// Use this when targeting a specific schema or custom-mapped table.
    /// </param>
    /// <returns>An instance of <see cref="IDeleteFromSingle{T}"/>.</returns>
    public static IDeleteFromSingle<T> DeleteFromSingle<T>(StormContext context, int variant, T value, string customQuotedObjectFullName) where T : IDataBindable
    {
        return new DeleteFromSingle<T>(context, variant, value, customQuotedObjectFullName);
    }

    /// <summary>
    /// Creates an instance of <see cref="IDeleteFromSingle{T}"/> for deleting multiple records from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to delete.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the delete operation.</param>
    /// <param name="values">The values to delete.</param>
    /// <returns>An instance of <see cref="IDeleteFromSingle{T}"/>.</returns>
    public static IDeleteFromSingle<T> DeleteFromSingle<T>(StormContext context, int variant, IEnumerable<T> values) where T : IDataBindable
    {
        return new DeleteFromSingle<T>(context, variant, values);
    }

    /// <summary>
    /// Creates an instance of <see cref="IDeleteFromSingle{T}"/> for deleting multiple records from a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to delete.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the delete operation.</param>
    /// <param name="values">The values to delete.</param>
    /// <param name="customQuotedObjectFullName">
    /// Fully-qualified and properly quoted table name.
    /// Use this when targeting a specific schema or custom-mapped table.
    /// </param>
    /// <returns>An instance of <see cref="IDeleteFromSingle{T}"/>.</returns>
    public static IDeleteFromSingle<T> DeleteFromSingle<T>(StormContext context, int variant, IEnumerable<T> values, string customQuotedObjectFullName) where T : IDataBindable
    {
        return new DeleteFromSingle<T>(context, variant, values, customQuotedObjectFullName);
    }

    #endregion Delete

    #region Update

    /// <summary>
    /// Creates an instance of <see cref="IUpdateFrom{T}"/> for updating data in a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to update.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the update operation.</param>
    /// <returns>An instance of <see cref="IUpdateFrom{T}"/>.</returns>
    public static IUpdateFrom<T> UpdateFrom<T>(StormContext context, int variant) where T : IDataBindable
    {
        return new UpdateFrom<T>(context, variant);
    }

    /// <summary>
    /// Creates an instance of <see cref="IUpdateFrom{T}"/> for updating data in a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to update.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the update operation.</param>
    /// <param name="customQuotedObjectFullName">
    /// Fully-qualified and properly quoted table name.
    /// Use this when targeting a specific schema or custom-mapped table.
    /// </param>
    /// <returns>An instance of <see cref="IUpdateFrom{T}"/>.</returns>
    public static IUpdateFrom<T> UpdateFrom<T>(StormContext context, int variant, string customQuotedObjectFullName) where T : IDataBindable
    {
        return new UpdateFrom<T>(context, variant, customQuotedObjectFullName);
    }

    /// <summary>
    /// Creates an instance of <see cref="IUpdateFromSingle{T}"/> for updating a single record in a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to update.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the update operation.</param>
    /// <param name="keyValues">The key values to identify the record.</param>
    /// <param name="keyId">ID of the unique index/primary key in KeyColumnDefs array.</param>
    /// <returns>An instance of <see cref="IUpdateFromSingle{T}"/>.</returns>
    public static IUpdateFromSingle<T> UpdateFromSingle<T>(StormContext context, int variant, object[] keyValues, int keyId) where T : IDataBindable
    {
        return new UpdateFromSingle<T>(context, variant, keyValues, keyId);
    }

    /// <summary>
    /// Creates an instance of <see cref="IUpdateFromSingle{T}"/> for updating a single record in a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to update.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the update operation.</param>
    /// <param name="keyValues">The key values to identify the record.</param>
    /// <param name="keyId">ID of the unique index/primary key in KeyColumnDefs array.</param>
    /// <param name="customQuotedObjectFullName">
    /// Fully-qualified and properly quoted table name.
    /// Use this when targeting a specific schema or custom-mapped table.
    /// </param>
    /// <returns>An instance of <see cref="IUpdateFromSingle{T}"/>.</returns>
    public static IUpdateFromSingle<T> UpdateFromSingle<T>(StormContext context, int variant, object[] keyValues, int keyId, string customQuotedObjectFullName) where T : IDataBindable
    {
        return new UpdateFromSingle<T>(context, variant, keyValues, keyId, customQuotedObjectFullName);
    }

    #endregion Update

    #region Insert

    /// <summary>
    /// Creates an instance of <see cref="IInsertInto{T}"/> for inserting data into a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to insert.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the insert operation.</param>
    /// <returns>An instance of <see cref="IInsertInto{T}"/>.</returns>
    public static IInsertInto<T> InsertInto<T>(StormContext context, int variant) where T : IDataBindable
    {
        return new InsertInto<T>(context, variant);
    }


    /// <summary>
    /// Creates an instance of <see cref="IInsertInto{T}"/> for inserting data into a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to insert.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the insert operation.</param>
    /// <param name="customQuotedObjectFullName">
    /// Fully-qualified and properly quoted table name.
    /// Use this when targeting a specific schema or custom-mapped table.
    /// </param>
    /// <returns>An instance of <see cref="IInsertInto{T}"/>.</returns>
    public static IInsertInto<T> InsertInto<T>(StormContext context, int variant, string customQuotedObjectFullName) where T : IDataBindable
    {
        return new InsertInto<T>(context, variant, customQuotedObjectFullName);
    }

    #endregion Insert

    #region Merge

    /// <summary>
    /// Creates an instance of <see cref="IMergeInto{T}"/> for merging data into a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to merge.</typeparam>
    /// <param name="context">The Storm context connection.</param>
    /// <param name="variant">The variant of the merge operation.</param>
    /// <returns>An instance of <see cref="IMergeInto{T}"/>.</returns>
    public static IMergeInto<T> MergeInto<T>(StormContext context, int variant) where T : IDataBindable
    {
        return new MergeInto<T>(context, variant);
    }

    /// <summary>
    /// Creates an instance of <see cref="IMergeInto{T}"/> for merging data into a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to merge.</typeparam>
    /// <param name="context">The Storm context connection.</param>
    /// <param name="variant">The variant of the merge operation.</param>
    /// <param name="customQuotedObjectFullName">
    /// Fully-qualified and properly quoted table name.
    /// Use this when targeting a specific schema or custom-mapped table.
    /// </param>
    /// <returns>An instance of <see cref="IMergeInto{T}"/>.</returns>
    public static IMergeInto<T> MergeInto<T>(StormContext context, int variant, string customQuotedObjectFullName) where T : IDataBindable
    {
        return new MergeInto<T>(context, variant, customQuotedObjectFullName);
    }
    #endregion Merge

    #region BulkInsert

    /// <summary>
    /// Creates an instance of <see cref="IBulkInsert{T}"/> for inserting bulk data into a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to insert.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the insert operation.</param>
    /// <returns>An instance of <see cref="IBulkInsert{T}"/>.</returns>
    public static IBulkInsert<T> BulkInsert<T>(StormContext context, int variant) where T : IDataBindable
    {
        return new BulkInsert<T>(context, variant);
    }

    /// <summary>
    /// Creates an instance of <see cref="IBulkInsert{T}"/> for inserting bulk data into a table.
    /// </summary>
    /// <typeparam name="T">The type of the data to insert.</typeparam>
    /// <param name="context">The Storm context.</param>
    /// <param name="variant">The variant of the insert operation.</param>
    /// <param name="customQuotedObjectFullName">
    /// Fully-qualified and properly quoted table name.
    /// Use this when targeting a specific schema or custom-mapped table.
    /// </param>
    /// <returns>An instance of <see cref="IBulkInsert{T}"/>.</returns>
    public static IBulkInsert<T> BulkInsert<T>(StormContext context, int variant, string customQuotedObjectFullName) where T : IDataBindable
    {
        return new BulkInsert<T>(context, variant, customQuotedObjectFullName);
    }

    #endregion
}
