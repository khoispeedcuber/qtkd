using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.Entities;

namespace QTKD.Api.Repositories;

public class ContractRepository : IContractRepository
{
    private readonly QtkdDbContext _db;

    public ContractRepository(QtkdDbContext db) => _db = db;

    public Task<Contract?> GetByIdWithCustomerAsync(int id, CancellationToken ct)
        => _db.Contracts
              .AsNoTracking()
              .Include(c => c.Customer)
              .FirstOrDefaultAsync(c => c.Id == id, ct);
}
