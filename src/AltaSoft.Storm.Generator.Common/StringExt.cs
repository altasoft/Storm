using System;
using System.Collections.Generic;

// ReSharper disable StringLiteralTypo

namespace AltaSoft.Storm.Generator.Common;

public static class StringExt
{
    /// <summary>
    /// Converts the first character of a string to lowercase and returns the modified string.
    /// </summary>
    /// <param name="self">The string to convert.</param>
    /// <returns>The modified string with the first character in lowercase.</returns>
    public static string ToCamelCase(this string self) => char.ToLowerInvariant(self[0]) + self.Substring(1);

    /// <summary>
    /// Converts a string to PascalCase format.
    /// </summary>
    /// <param name="value">The input string to be converted.</param>
    /// <returns>
    /// The input string converted to PascalCase format.
    /// </returns>
    public static string ToPascalCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var result = "";
        foreach (var word in value.Split([' ', '-', '_'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.Length > 1)
                result += char.ToUpperInvariant(word[0]) + word.Substring(1);
            else
                result += word.ToUpperInvariant();
        }

        return result;
    }

    /// <summary>
    /// Quotes the SQL name if it is not already quoted.
    /// </summary>
    /// <param name="self">The SQL name to quote.</param>
    /// <returns>The quoted SQL name.</returns>
    public static string QuoteSqlName(this string self) => self.IsSqlNameQuoted() ? self : self.QuoteName('[');

    /// <summary>
    /// Determines if the given string is a SQL name that is enclosed in square brackets.
    /// </summary>
    /// <param name="self">The string to check.</param>
    /// <returns>True if the string is a SQL name enclosed in square brackets, false otherwise.</returns>
    public static bool IsSqlNameQuoted(this string self) => self.Length > 0 && self[0] == '[' && self[self.Length - 1] == ']';

    /// <summary>
    /// Returns a string with the delimiters added to make the input string
    /// a valid SQL Server delimited identifier.
    /// An ArgumentException is thrown for invalid arguments.
    /// </summary>
    /// <param name="name">sysname, limited to 128 characters.</param>
    /// <param name="quoteCharacter">Can be a single quotation mark ( ' ), a
    /// left or right bracket ( [] ), or a double quotation mark ( " ).</param>
    /// <returns>An escaped identifier, no longer than 258 characters.</returns>
    public static string QuoteName(this string? name, char quoteCharacter)
    {
        name ??= string.Empty;
        const int sysNameLength = 128;
        if (name.Length > sysNameLength)
        {
            throw new ArgumentException($"{nameof(name)} is longer than {sysNameLength} characters", nameof(name));
        }

        return quoteCharacter switch
        {
            '\'' => $"'{name.Replace("'", "''")}'",
            '"' => $"\"{name.Replace("\"", "\"\"")}\"",
            '[' or ']' => $"[{name.Replace("]", "]]")}]",
            _ => throw new ArgumentException($"{nameof(quoteCharacter)} must be one of: ', \", [, or ]", nameof(name))
        };
    }

    private static readonly Dictionary<string, string> s_irregularPlurals = new(StringComparer.Ordinal)
    {
        {"addendum", "addenda"},
        {"aircraft", "aircraft"},
        {"alga", "algae"},
        {"alumna", "alumnae"},
        {"alumnus", "alumni"},
        {"analysis", "analyses"},
        {"antenna", "antennae"},
        {"appendix", "appendices"},
        {"axis", "axes"},
        {"bacterium", "bacteria"},
        {"basis", "bases"},
        {"beau", "beaux"},
        {"bison", "bison"},
        {"bureau", "bureaus"},
        {"cactus", "cacti"},
        {"child", "children"},
        {"codex", "codices"},
        {"concerto", "concerti"},
        {"corpus", "corpora"},
        {"criterion", "criteria"},
        {"curriculum", "curricula"},
        {"datum", "data"},
        {"deer", "deer"},
        {"diagnosis", "diagnoses"},
        {"die", "dice"},
        {"dwarf", "dwarves"},
        {"echo", "echoes"},
        {"elf", "elves"},
        {"emphasis", "emphases"},
        {"erratum", "errata"},
        {"faux pas", "faux pas"},
        {"fish", "fish"},
        {"focus", "foci"},
        {"foot", "feet"},
        {"formula", "formulae"},
        {"fungus", "fungi"},
        {"genus", "genera"},
        {"goose", "geese"},
        {"graffito", "graffiti"},
        {"hippopotamus", "hippopotami"},
        {"hypothesis", "hypotheses"},
        {"index", "indices"},
        {"larva", "larvae"},
        {"libretto", "libretti"},
        {"loaf", "loaves"},
        {"locus", "loci"},
        {"louse", "lice"},
        {"man", "men"},
        {"matrix", "matrices"},
        {"medium", "media"},
        {"memorandum", "memoranda"},
        {"minutia", "minutiae"},
        {"moose", "moose"},
        {"mouse", "mice"},
        {"nebula", "nebulae"},
        {"nucleus", "nuclei"},
        {"oasis", "oases"},
        {"octopus", "octopi"},
        {"opus", "opera"},
        {"ovum", "ova"},
        {"ox", "oxen"},
        {"parenthesis", "parentheses"},
        {"phenomenon", "phenomena"},
        {"phylum", "phyla"},
        {"quiz", "quizzes"},
        {"radius", "radii"},
        {"referendum", "referenda"},
        {"salmon", "salmon"},
        {"scarf", "scarves"},
        {"self", "selves"},
        {"series", "series"},
        {"sheep", "sheep"},
        {"shrimp", "shrimp"},
        {"species", "species"},
        {"stimulus", "stimuli"},
        {"stratum", "strata"},
        {"swine", "swine"},
        {"syllabus", "syllabi"},
        {"synopsis", "synopses"},
        {"tableau", "tableaux"},
        {"thesis", "theses"},
        {"thief", "thieves"},
        {"tooth", "teeth"},
        {"trout", "trout"},
        {"tuna", "tuna"},
        {"vertebra", "vertebrae"},
        {"virus", "viruses"},
        {"woman", "women"},
        {"wolf", "wolves"}
    };

    /// <summary>
    /// Pluralizes singular word
    /// </summary>
    /// <param name="word">Word to pluralize</param>
    /// <returns>Pluralized word</returns>
    public static string Pluralize(this string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return word;
        }

        // Check for irregular plurals
        if (s_irregularPlurals.TryGetValue(word.ToLowerInvariant(), out var irregularPlural))
        {
            return irregularPlural;
        }

        // Handling the 'y' to 'ies'
        if (word.EndsWith("y", StringComparison.OrdinalIgnoreCase) &&
            word.Length > 1 &&
            !IsVowel(word[word.Length - 2]))
        {
            return word.Substring(0, word.Length - 1) + "ies";
        }

        // Handling words ending in 'o'
        if (word.EndsWith("o", StringComparison.OrdinalIgnoreCase))
        {
            // This is a simplification and may not work for all cases
            return word + "es";
        }

        // For words ending in 's', 'x', 'z', 'ch', 'sh', add 'es'
        if (word.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
            word.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
        {
            return word + "es";
        }

        // Default case
        return word + "s";
    }

    private static bool IsVowel(char c)
    {
        return "aeiou".IndexOf(char.ToLowerInvariant(c)) >= 0;
    }
}
