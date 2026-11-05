using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.Entities;

namespace QTKD.Api.Repositories;

public class PriceListRepository : RepositoryBase, IPriceListRepository
{
    public PriceListRepository(QtkdDbContext db) : base(db) { }

    public async Task<IReadOnlyList<PriceList>> GetAllWithItemsAsync(CancellationToken ct)
        => await Db.PriceLists
                   .AsNoTracking()
                   .Include(p => p.Items)
                   .OrderBy(p => p.Id)
                   .ToListAsync(ct);

    public Task<PriceList?> GetByIdWithItemsAsync(int id, CancellationToken ct)
        => Db.PriceLists
             .AsNoTracking()
             .Include(p => p.Items)
             .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct)
        => Db.PriceLists.AnyAsync(p => p.Id == id, ct);

    public Task<bool> ItemExistsAsync(int priceListId, string serviceCode, CancellationToken ct)
        => Db.PriceItems.AnyAsync(i => i.PriceListId == priceListId && i.ServiceCode == serviceCode, ct);

    public void Add(PriceList priceList) => Db.PriceLists.Add(priceList);

    public void AddItem(PriceItem item) => Db.PriceItems.Add(item);
}
