using System;
using AltaSoft.Storm.Attributes;

// ReSharper disable InconsistentNaming

namespace AltaSoft.Storm.TestModels.AdventureWorks;

[StormDbObject<AdventureWorksStormContext>(SchemaName = "Production", ObjectName = "Product")]
public partial record Product
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey | ColumnType.AutoIncrement)]
    public int ProductID { get; set; }
    [StormColumn(ColumnType = ColumnType.PrimaryKey, DbType = UnifiedDbType.String, Size = 50)]
    public string Name { get; set; }
    [StormColumn(ColumnType = ColumnType.PrimaryKey, DbType = UnifiedDbType.String, Size = 25)]
    public string ProductNumber { get; set; }
    [StormColumn(ColumnType = ColumnType.HasDefaultValue)]
    public bool MakeFlag { get; set; }
    [StormColumn(ColumnType = ColumnType.HasDefaultValue)]
    public bool FinishedGoodsFlag { get; set; }
    [StormColumn(DbType = UnifiedDbType.String, Size = 15)]
    public string? Color { get; set; }
    public short SafetyStockLevel { get; set; }
    public short ReorderPoint { get; set; }
    [StormColumn(DbType = UnifiedDbType.Decimal)]
    public decimal StandardCost { get; set; }
    [StormColumn(DbType = UnifiedDbType.Decimal)]
    public decimal ListPrice { get; set; }
    [StormColumn(DbType = UnifiedDbType.String, Size = 5)]
    public string? Size { get; set; }
    [StormColumn(DbType = UnifiedDbType.StringFixedLength, Size = 3)]
    public string? SizeUnitMeasureCode { get; set; }
    [StormColumn(DbType = UnifiedDbType.StringFixedLength, Size = 3)]
    public string? WeightUnitMeasureCode { get; set; }
    [StormColumn(DbType = UnifiedDbType.Decimal, Precision = 8, Scale = 2)]
    public decimal? Weight { get; set; }
    public int DaysToManufacture { get; set; }
    [StormColumn(DbType = UnifiedDbType.StringFixedLength, Size = 2)]
    public string? ProductLine { get; set; }
    [StormColumn(DbType = UnifiedDbType.StringFixedLength, Size = 2)]
    public string? Class { get; set; }
    [StormColumn(DbType = UnifiedDbType.StringFixedLength, Size = 2)]
    public string? Style { get; set; }
    public int? ProductSubcategoryID { get; set; }
    public int? ProductModelID { get; set; }

    [StormColumn(DbType = UnifiedDbType.DateTime)]
    public DateTime SellStartDate { get; set; }
    public DateTime? SellEndDate { get; set; }
    public DateTime? DiscontinuedDate { get; set; }
    [StormColumn(ColumnName = "rowguid", ColumnType = ColumnType.PrimaryKey | ColumnType.HasDefaultValue)]
    public Guid Rowguid { get; set; }
    [StormColumn(ColumnType = ColumnType.HasDefaultValue)]
    public DateTime ModifiedDate { get; set; }
}
