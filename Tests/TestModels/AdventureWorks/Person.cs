using System;
using System.ComponentModel.DataAnnotations.Schema;
using AltaSoft.Storm.Attributes;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

namespace AltaSoft.Storm.TestModels.AdventureWorks;

[StormDbObject<AdventureWorksStormContext>(SchemaName = "Person", ObjectName = "Person")]
[Table("Person", Schema = "Person")]
public partial record Person
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public int BusinessEntityID { get; set; }
    [StormColumn(DbType = UnifiedDbType.StringFixedLength, Size = 2)]
    public string PersonType { get; set; }
    [StormColumn(ColumnType = ColumnType.HasDefaultValue)]
    public bool NameStyle { get; set; }
    [StormColumn(DbType = UnifiedDbType.String, Size = 8)]
    public string? Title { get; set; }
    [StormColumn(DbType = UnifiedDbType.String, Size = 50)]
    public string FirstName { get; set; }
    [StormColumn(DbType = UnifiedDbType.String, Size = 50)]
    public string? MiddleName { get; set; }
    [StormColumn(DbType = UnifiedDbType.String, Size = 50)]
    public string LastName { get; set; }
    [StormColumn(DbType = UnifiedDbType.String, Size = 10)]
    public string? Suffix { get; set; }
    [StormColumn(ColumnType = ColumnType.HasDefaultValue)]
    public int EmailPromotion { get; set; }
    //[StormColumn(SaveAs = SaveAs.Xml)]
    //public string? AdditionalContactInfo { get; set; }
    //[StormColumn(SaveAs = SaveAs.Xml)]
    //public string? Demographics { get; set; }
    [StormColumn(ColumnName = "rowguid", ColumnType = ColumnType.HasDefaultValue)]
    public Guid Rowguid { get; set; }
    [StormColumn(ColumnType = ColumnType.HasDefaultValue)]
    public DateTime ModifiedDate { get; set; }
}
