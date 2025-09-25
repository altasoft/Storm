using System;
using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

[StormDbObject<TestStormContext>]
[StormIndex(["Text"], false)]
public sealed partial record Post
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey)]
    public int Id { get; set; }

    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = -1)]
    public string Text { get; set; }
    public DateTime CreationDate { get; set; }
    public DateTime LastChangeDate { get; set; }
    public int? Counter1 { get; set; }
    public int? Counter2 { get; set; }
    public int? Counter3 { get; set; }
    public int? Counter4 { get; set; }
    public int? Counter5 { get; set; }
    public int? Counter6 { get; set; }
    public int? Counter7 { get; set; }
    public int? Counter8 { get; set; }
    public int? Counter9 { get; set; }

    [StormColumn(Size = 4000)]
    public byte[] ImageCVT { get; set; }

    /// <summary>
    /// Default constructor.
    /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public Post()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }
}

[StormDbObject<TestStormContext>(SchemaName = "su", ObjectName = "RefIdentifiers")]
[StormIndex(["Eid", "Id", "IdType", "Script"], true)]
public partial record RefIdentifiers
{
    [StormColumn(ColumnType = ColumnType.PrimaryKey, DbType = UnifiedDbType.AnsiString, Size = 12)]
    public string RecordKey { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 7)]
    public string Eid { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 7)]
    public string ParentEid { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 7)]
    public string GroupEid { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 2)]
    public string EntityType { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 2)]
    public string CountryCode { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string? PaymentAreaCodes { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 35)]
    public string Id { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 20)]
    public string IdType { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 5)]
    public string? IsoClcType { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 255)]
    public string? IdUsage { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 2)]
    public string? FinancialType { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 4)]
    public string? SwiftType { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string? SuccessorId { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string? DomesticAchId { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 11)]
    public string? FinPlusBic { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 255)]
    public string? FinPlusDn { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 50)]
    public string? IbanId { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 11)]
    public string? IbanBic { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 140)]
    public string? Name { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 140)]
    public string? AlternativeName { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 100)]
    public string CountryName { get; set; }
    [StormColumn(DbType = UnifiedDbType.AnsiString, Size = 20)]
    public string Script { get; set; }
}
