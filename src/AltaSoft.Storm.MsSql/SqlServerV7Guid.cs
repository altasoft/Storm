#if NET9_0_OR_GREATER

using System;

namespace AltaSoft.Storm
{
    /// <summary>
    /// Provides methods for generating and converting RFC 4122 version 7 GUIDs
    /// with SQL Server-friendly byte ordering for time-based sorting.
    /// </summary>
    public static class SqlServerV7Guid
    {
        /// <summary>
        /// Generates a new version 7 GUID whose underlying byte order
        /// is optimized for SQL Server's byte-wise comparison, ensuring
        /// that GUIDs sort by creation time.
        /// </summary>
        /// <returns>
        /// A <see cref="Guid"/> value with SQL Server-friendly byte ordering.
        /// </returns>
        public static Guid New()
        {
            var g = Guid.CreateVersion7(); // RFC v7 (time-ordered in string form)
            var b = g.ToByteArray(); // little-endian segments (Data1/2/3)

            // Flip the endian-ness of the first three fields so SQL's byte-wise compare matches time order
            Array.Reverse(b, 0, 4); // Data1
            Array.Reverse(b, 4, 2); // Data2
            Array.Reverse(b, 6, 2); // Data3

            return new Guid(b); // This Guid’s underlying bytes are SQL-sort-friendly
        }

        /// <summary>
        /// Converts a SQL Server-ordered version 7 GUID back to its canonical
        /// RFC 4122 representation, suitable for display or interoperability.
        /// </summary>
        /// <param name="sqlOrdered">
        /// The <see cref="Guid"/> value with SQL Server-friendly byte ordering.
        /// </param>
        /// <returns>
        /// The canonical RFC 4122 version 7 <see cref="Guid"/> value.
        /// </returns>
        public static Guid ToCanonical(Guid sqlOrdered)
        {
            var b = sqlOrdered.ToByteArray();
            Array.Reverse(b, 0, 4);
            Array.Reverse(b, 4, 2);
            Array.Reverse(b, 6, 2);
            return new Guid(b); // same value, but canonical v7 ordering for strings
        }
    }
}

#endif
