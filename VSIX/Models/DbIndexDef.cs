using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace AltaSoft.Storm.Models;

internal sealed class DbIndexDef
{
    public int Id { get; }
    public string IndexName { get; set; }
    public bool IsUnique { get; set; }
    public string[] Columns { get; set; }

    public DbObjectDef ParentDbObject { get; internal set; } = default!;

    public ImageMoniker IsKeyImage => IsUnique ? KnownMonikers.Key : KnownMonikers.Blank;

    public DbIndexDef(SqlDataReader reader)
    {
        Id = (int)reader["object_id"];
        IndexName = (string)reader["name"];
        IsUnique = (bool)reader["is_unique"];
        var columns = (string)reader["index_columns"];
        Columns = columns.Split('|');
    }

    // For Dummy
    private DbIndexDef()
    {
        IndexName = "Loading...";
        Columns = [];
    }

    public static DbIndexDef CreateDummy() => new();
}
