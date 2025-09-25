using System;
using System.Collections.Generic;
using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.Extensions;

/// <summary>
/// Extension methods for the (StormColumnDef column, object? value).
/// </summary>
internal static class StormColumnDefAndValuesExt
{
    /// <summary>
    /// Retrieves the master and detail columns for insertion based on the provided column values.
    /// </summary>
    /// <param name="columnValues">The array of column values.</param>
    /// <returns>A tuple containing the master columns and optional detail columns.</returns>
    public static (List<(StormColumnDef column, object? value)> masterColumns, List<(StormColumnDef column, object value)>? detailColumns) GetMaterAndDetailColumnsForInsert(this (StormColumnDef column, object? value)[] columnValues)
    {
        List<(StormColumnDef column, object? value)> masterColumns = new();
        List<(StormColumnDef column, object value)>? detailColumns = null;

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < columnValues.Length; i++)
        {
            var columnValue = columnValues[i];
            var column = columnValue.column;

            if ((column.Flags & StormColumnFlags.CanInsert) == StormColumnFlags.None)
                continue;

            if (column.SaveAs != SaveAs.DetailTable)
            {
                masterColumns.Add(columnValue);
            }
            else
            if (columnValue.value is not null)
            {
                detailColumns ??= new List<(StormColumnDef column, object value)>(4);
                detailColumns.Add(columnValue!);
            }
        }

        return (masterColumns, detailColumns?.Count > 0 ? detailColumns : null);
    }

    /// <summary>
    /// Retrieves the columns and their corresponding values that are eligible for insertion into a database table.
    /// </summary>
    /// <param name="columnValues">The array of column-value pairs.</param>
    /// <returns>An array of column-value pairs that are eligible for insertion.</returns>
    public static (StormColumnDef column, object? value)[] GetColumnsForInsert(this (StormColumnDef column, object? value)[] columnValues)
        => Array.FindAll(columnValues, x => x.column.CanInsertColumn());

    /// <summary>
    /// Retrieves the master and detail columns for update from the given column values.
    /// </summary>
    /// <param name="columnValues">The column values.</param>
    /// <returns>A tuple containing the master columns and optional detail columns.</returns>
    public static (List<(StormColumnDef column, object? value)> masterColumns, List<(StormColumnDef column, object value)>? detailColumns) GetMaterDetailColumnsForUpdate(this (StormColumnDef column, object? value)[] columnValues)
    {
        List<(StormColumnDef column, object? value)> masterColumns = new();
        List<(StormColumnDef column, object value)>? detailColumns = null;

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < columnValues.Length; i++)
        {
            var columnValue = columnValues[i];
            var column = columnValue.column;

            if ((column.Flags & StormColumnFlags.CanUpdate) == StormColumnFlags.None)
                continue;

            if (column.SaveAs != SaveAs.DetailTable)
            {
                masterColumns.Add(columnValue);
            }
            else
            if (columnValue.value is not null)
            {
                detailColumns ??= [];
                detailColumns.Add(columnValue!);
            }
        }

        return (masterColumns, detailColumns?.Count > 0 ? detailColumns : null);
    }

    /// <summary>
    /// Retrieves the columns and their corresponding values that are eligible for update in a database table.
    /// </summary>
    /// <param name="columnValues">The array of column-value pairs.</param>
    /// <returns>An array of column-value pairs that are eligible for update.</returns>
    public static (StormColumnDef column, object? value)[] GetColumnsForUpdate(this (StormColumnDef column, object? value)[] columnValues)
        => Array.FindAll(columnValues, x => x.column.CanUpdateColumn());
}
