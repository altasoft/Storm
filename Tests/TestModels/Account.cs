using AltaSoft.Storm.Attributes;
using AltaSoft.Storm.TestModels.VeryBadNamespace;

namespace AltaSoft.Storm.TestModels
{
    [StormDbObject<TestStormContext>(ObjectName = "Accounts", BulkInsert = true)]
    [StormIndex([nameof(IbanAccount), nameof(Ccy)], isUnique: true)]
    public sealed partial record Account
    {
        [StormColumn(ColumnType = ColumnType.PrimaryKey)]
        public required int Id { get; set; }

        public required CustomerId RelatedCustomerId { get; set; }

        public CustomerId? OwnerCustomerId { get; set; }

        [StormColumn(DbType = UnifiedDbType.AnsiStringFixedLength, Size = 3)]
        public required string Ccy { get; set; }

        [StormColumn(DbType = UnifiedDbType.AnsiStringFixedLength, Size = 22)]
        public required string IbanAccount { get; set; }

        public required long BbanAccount { get; set; }

        public required int BranchId { get; set; }

        public required byte Type { get; set; }

        public int? SubType { get; set; }


        [StormColumn(DbType = UnifiedDbType.String, Size = 50)]
        public string? FriendlyName { get; set; }

        [StormColumn(DbType = UnifiedDbType.String, Size = 100)]
        public required string Name { get; set; }

        [StormColumn(DbType = UnifiedDbType.String, Size = 100)]
        public string? ProductName { get; set; }

        /// <summary>
        /// Can debit this account (if only budget, then true)
        /// </summary>
        public bool CanDebit { get; set; }

        /// <summary>
        /// Can credit this account
        /// </summary>
        public bool CanCredit { get; set; }
    }
}
