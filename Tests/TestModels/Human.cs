using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

[StormDbObject<TestStormContext>(ObjectName = "Humans", DisplayName = "Human")]
[StormDbObject<TestStormContext>(DisplayName = "HumanVV", ObjectType = DbObjectType.VirtualView, VirtualViewSql = "SELECT * FROM {%schema%}.Humans")]
[StormDbObject<TestStormContext>(DisplayName = "HumanVVNoSql", ObjectType = DbObjectType.CustomSqlStatement, UpdateMode = UpdateMode.NoUpdates, BulkInsert = false)]
public partial record Human
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)] // Indicates no identity or auto-increment
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public long XId { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string Name { get; set; }
    public long Age { get; set; }

    [StormColumn(DbType = UnifiedDbType.Currency)]
    public decimal Amount { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiStringFixedLength, Size = 3)]
    public string Ccy { get; set; }

    public override string ToString()
    {
        return $"{nameof(XId)}: {XId}, {nameof(Name)}: {Name}";
    }
}
