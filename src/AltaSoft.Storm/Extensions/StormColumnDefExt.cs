using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.Extensions;

/// <summary>
/// Extension methods for the StormColumnDef class.
/// </summary>
internal static class StormColumnDefExt
{
    /// <summary>
    /// Filters and selects columns based on certain conditions.
    /// </summary>
    /// <param name="columns">The array of StormColumnDef objects.</param>
    /// <param name="partialLoadFlags">The partial load flags.</param>
    /// <param name="tableAlias">The table alias.</param>
    /// <returns>An IEnumerable of strings representing the selected columns.</returns>
    public static IEnumerable<string> GetSelectableColumns(this StormColumnDef[] columns, uint partialLoadFlags, string? tableAlias)
    {
        tableAlias = tableAlias is null ? null : tableAlias + '.';

        return columns.FilterAndSelect(
            static x => x.CanSelectColumn(),
            x => x.PartialLoadFlags == 0 || (x.PartialLoadFlags & partialLoadFlags) != 0 ? tableAlias + x.ColumnName : "NULL");
    }

    /// <summary>
    /// Determines if the detail tables should be loaded based on the partial load flags.
    /// </summary>
    /// <param name="columns">The array of StormColumnDef objects.</param>
    /// <param name="partialLoadFlags">The partial load flags to check against.</param>
    /// <returns>True if the details should be loaded, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldLoadDetails(this StormColumnDef[] columns, uint partialLoadFlags) => Array.Exists(columns, x => x.LoadThisDetailColumnInPartialLoading(partialLoadFlags));

    /// <summary>
    /// Returns a filtered collection of StormColumnDef objects that have SaveAs property set to DetailTable and PartialLoadFlags property either equal to 0 or matching the provided partialLoadFlags.
    /// </summary>
    /// <param name="columns">The array of StormColumnDef objects to filter.</param>
    /// <param name="partialLoadFlags">The partial load flags to match against the PartialLoadFlags property of StormColumnDef objects.</param>
    /// <returns>A filtered collection of StormColumnDef objects.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<StormColumnDef> GetDetailColumns(this StormColumnDef[] columns, uint partialLoadFlags) => columns.Where(x => x.LoadThisDetailColumnInPartialLoading(partialLoadFlags));

    /// <summary>
    /// Returns an IEnumerable of StormColumnDef objects that have the SaveAs property set to SaveAs.DetailTable.
    /// </summary>
    /// <param name="columns">An array of StormColumnDef objects.</param>
    /// <returns>An IEnumerable of StormColumnDef objects that have the SaveAs property set to SaveAs.DetailTable.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<StormColumnDef> GetDetailColumns(this StormColumnDef[] columns) => columns.Where(x => x.SaveAs == SaveAs.DetailTable);
}
