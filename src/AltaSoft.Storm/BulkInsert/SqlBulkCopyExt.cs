using System;
using System.Globalization;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace AltaSoft.Storm.BulkInsert;

/// <summary>
/// Helper class to process the SqlBulkCopy class
/// </summary>
internal static class SqlBulkCopyExt
{
    private static FieldInfo? s_rowsCopiedField;

    /// <summary>
    /// Gets the rows copied from the specified SqlBulkCopy object
    /// </summary>
    /// <param name="bulkCopy">The bulk copy.</param>
    /// <returns></returns>
    public static int GetRowsCopied(this SqlBulkCopy bulkCopy)
    {
        s_rowsCopiedField ??= typeof(SqlBulkCopy).GetField("_rowsCopied", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);

        return Convert.ToInt32(s_rowsCopiedField!.GetValue(bulkCopy)!, CultureInfo.InvariantCulture);
    }
}
