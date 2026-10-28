using Microsoft.EntityFrameworkCore;
using QTKD.Api.Entities;

namespace QTKD.Api.Data;

/// <summary>
/// Thêm dữ liệu mẫu cho môi trường Development.
/// - Chỉ chạy khi database ĐÃ migrate xong.
/// - Idempotent: đã có Customer thì không thêm lại.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QtkdDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("QTKD.Seed");

        var applied = await db.Database.GetAppliedMigrationsAsync(ct);
        var pending = await db.Database.GetPendingMigrationsAsync(ct);

        if (!applied.Any() || pending.Any())
        {
            logger.LogWarning(
                "Database chưa migrate đầy đủ nên bỏ qua seed. Chạy: dotnet ef database update --project src/QTKD.Api");
            return;
        }

        if (await db.Customers.AnyAsync(ct))
        {
            logger.LogInformation("Database đã có dữ liệu, bỏ qua seed.");
            return;
        }

        var minhPhat = new Customer { Name = "Công ty Minh Phát", Status = CustomerStatus.Active };
        var haiDang  = new Customer { Name = "Công ty Hải Đăng",  Status = CustomerStatus.Active };
        db.Customers.AddRange(minhPhat, haiDang);

        db.Contracts.Add(new Contract
        {
            ContractNo = "HD-2026-018",
            Customer = minhPhat,
            StartDate = new DateOnly(2026, 11, 1),
            EndDate = new DateOnly(2027, 10, 31),
            Status = ContractStatus.Draft
        });

        db.PriceLists.Add(new PriceList
        {
            Name = "Bảng giá dịch vụ cảng 2026",
            EffectiveDate = new DateOnly(2026, 11, 1),
            Items =
            {
                new PriceItem { ServiceCode = "CONT-HANDLING", UnitPrice = 850_000m },
                new PriceItem { ServiceCode = "STORAGE-DAY",   UnitPrice = 120_000m }
            }
        });

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Đã thêm dữ liệu mẫu: 2 Customer, 1 Contract (Draft), 1 PriceList (2 PriceItem).");
    }
}
