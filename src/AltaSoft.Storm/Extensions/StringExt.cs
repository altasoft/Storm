using System;
using System.Data.Common;
using System.Globalization;
using AltaSoft.Storm.Exceptions;

namespace AltaSoft.Storm.Extensions;

/// <summary>
/// Contains extension methods for working with SQL Server delimited identifiers.
/// </summary>
internal static class StringExt
{
    /// <summary>
    /// Returns a string with the delimiters added to make the input string
    /// a valid SQL Server delimited identifier.
    /// An ArgumentException is thrown for invalid arguments.
    /// </summary>
    /// <param name="self">sysname, limited to 128 characters.</param>
    /// <returns>An escaped identifier, no longer than 128 characters.</returns>
    public static string QuoteSqlName(this string self)
    {
        self = self.QuoteName(StormManager.QuoteCharacter);

        if (self.Length > StormManager.MaxSysNameLength)
            throw new StormException($"Quoted object name is longer than {StormManager.MaxSysNameLength.ToString(CultureInfo.InvariantCulture)} characters");
        return self;
    }

    /// <summary>
    /// Removes the quoting characters from a SQL name if it is quoted.
    /// </summary>
    /// <param name="self">The SQL name to unquote.</param>
    /// <returns>The unquoted SQL name.</returns>
    public static string UnquoteSqlName(this string self)
    {
        var quoteCharacter = StormManager.QuoteCharacter;

        if (!self.IsNameQuoted(quoteCharacter))
            return self;

        return quoteCharacter == '[' ? self.Trim('[', ']') : self.Trim(quoteCharacter);
    }

    /// <summary>
    /// Returns a string with the delimiters added to make the input string quoted
    /// </summary>
    /// <param name="self">sysname, limited to 128 characters.</param>
    /// <param name="quoteCharacter">Single quotation mark.</param>
    /// <returns>An escaped identifier, no longer than 128 characters.</returns>
    public static string QuoteName(this string self, char quoteCharacter)
    {
        if (self.IsNameQuoted(quoteCharacter))
            return self;

        if (quoteCharacter == '[')
            return '[' + self + ']';

        return quoteCharacter + self + quoteCharacter;
    }

    /// <summary>
    /// Removes the quotes from a string if it is quoted with the specified quote character.
    /// </summary>
    /// <param name="self">The string to unquote.</param>
    /// <param name="quoteCharacter">The quote character used to enclose the string.</param>
    /// <returns>The unquoted string.</returns>
    public static string UnquoteName(this string self, char quoteCharacter)
    {
        if (!self.IsNameQuoted(quoteCharacter))
            return self;

        return quoteCharacter == '[' ? self.Trim('[', ']') : self.Trim(quoteCharacter);
    }

    /// <summary>
    /// Checks if the given string is quoted with the specified quote character.
    /// </summary>
    /// <param name="self">The string to check.</param>
    /// <param name="quoteCharacter">The quote character to check for.</param>
    /// <returns>True if the string is quoted with the specified quote character, false otherwise.</returns>
    public static bool IsNameQuoted(this string self, char quoteCharacter)
    {
        if (self.Length == 0)
            return false;

        if (quoteCharacter == '[')
            return self[0] == '[' && self[^1] == ']';

        return self[0] == quoteCharacter && self[^1] == quoteCharacter;
    }

    /// <summary>
    /// Determines whether two connection strings refer to the same database by comparing their
    /// <c>DataSource</c> and <c>InitialCatalog</c> properties.
    /// </summary>
    /// <param name="connectionStringA">The first connection string to compare.</param>
    /// <param name="connectionStringB">The second connection string to compare.</param>
    /// <returns>
    /// <c>true</c> if both connection strings have the same <c>DataSource</c> and <c>InitialCatalog</c> values; otherwise, <c>false</c>.
    /// </returns>
    internal static bool IsSameConnectionString(this string connectionStringA, string connectionStringB)
    {
        if (ReferenceEquals(connectionStringA, connectionStringB))
            return true;

        var a = new DbConnectionStringBuilder { ConnectionString = connectionStringA };
        var b = new DbConnectionStringBuilder { ConnectionString = connectionStringB };

        var serverA = GetValue(a, "Data Source", "Server");
        var serverB = GetValue(b, "Data Source", "Server");

        var dbA = GetValue(a, "Initial Catalog", "Database");
        var dbB = GetValue(b, "Initial Catalog", "Database");

        return string.Equals(serverA, serverB, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(dbA, dbB, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetValue(DbConnectionStringBuilder b, string key1, string key2)
    {
        if (b.TryGetValue(key1, out var value1))
            return value1.ToString()!;
        return b.TryGetValue(key2, out var value2) ? value2.ToString()! : string.Empty;
    }
}
