using AltaSoft.Storm.Generator.Common;

namespace AltaSoft.Storm.Models;

internal sealed class DbEntityDef
{
    public string Name { get; set; }
    public DupDbObjectType Type { get; set; }

    public DbObjectDefList DbObjects { get; set; }

    public DbEntityDef(string name, DupDbObjectType type)
    {
        Name = name;
        Type = type;
        DbObjects = new DbObjectDefList();
    }
}
