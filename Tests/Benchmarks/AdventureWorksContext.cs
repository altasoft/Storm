using AltaSoft.Storm.TestModels.AdventureWorks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AltaSoft.Storm.Benchmarks;

public class AdventureWorksContext : DbContext
{
    public static readonly SqlConnection Connection = GetSqlConnection();

    public AdventureWorksContext() : base(GetOptions())
    {
        // No-op.
    }

    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Person>(entity =>
        {
            entity.HasKey(e => e.BusinessEntityID);

            // Configure 'XId' as a non-identity column
            entity.Property(e => e.BusinessEntityID)
                .ValueGeneratedNever(); // This is important
        });
    }

    private static DbContextOptions<AdventureWorksContext> GetOptions()
    {
        return new DbContextOptionsBuilder<AdventureWorksContext>()
            .UseSqlServer(Connection)
            .Options;
    }

    private static SqlConnection GetSqlConnection()
    {
        var connection = new SqlConnection(Constants.ConnectionString);
        connection.Open();
        return connection;
    }
}
