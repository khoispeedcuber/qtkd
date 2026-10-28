using Microsoft.EntityFrameworkCore;
using QTKD.Api.Entities;

namespace QTKD.Api.Data;

public class QtkdDbContext : DbContext
{
    public QtkdDbContext(DbContextOptions<QtkdDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceItem> PriceItems => Set<PriceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Contract>(e =>
        {
            e.Property(x => x.ContractNo).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.ContractNo).IsUnique();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

            e.HasOne(x => x.Customer)
             .WithMany(c => c.Contracts)
             .HasForeignKey(x => x.CustomerId)
             .OnDelete(DeleteBehavior.Restrict);   // không cho xóa Customer đang có hợp đồng

            e.ToTable(t => t.HasCheckConstraint(
                "CK_Contracts_StartDate_LE_EndDate", "\"StartDate\" <= \"EndDate\""));
        });

        modelBuilder.Entity<PriceList>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(200);

            e.HasMany(x => x.Items)
             .WithOne(i => i.PriceList)
             .HasForeignKey(i => i.PriceListId)
             .OnDelete(DeleteBehavior.Cascade);    // xóa bảng giá thì xóa luôn các dòng giá
        });

        modelBuilder.Entity<PriceItem>(e =>
        {
            e.HasKey(x => new { x.PriceListId, x.ServiceCode });
            e.Property(x => x.ServiceCode).IsRequired().HasMaxLength(50);
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);

            e.ToTable(t => t.HasCheckConstraint(
                "CK_PriceItems_UnitPrice_NonNegative", "\"UnitPrice\" >= 0"));
        });
    }
}
