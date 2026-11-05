using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.Entities;

namespace QTKD.Api.Repositories;

public class ContractRepository : RepositoryBase, IContractRepository
{
    public ContractRepository(QtkdDbContext db) : base(db) { }

    public async Task<IReadOnlyList<Contract>> GetAllWithCustomerAsync(CancellationToken ct)
        => await Db.Contracts
                   .AsNoTracking()
                   .Include(c => c.Customer)
                   .OrderBy(c => c.Id)
                   .ToListAsync(ct);

    public Task<Contract?> GetByIdWithCustomerAsync(int id, CancellationToken ct)
        => Db.Contracts
             .AsNoTracking()
             .Include(c => c.Customer)
             .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Contract?> GetForUpdateAsync(int id, CancellationToken ct)
        => Db.Contracts
             .Include(c => c.Customer)
             .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ContractNoExistsAsync(string contractNo, CancellationToken ct)
        => Db.Contracts.AnyAsync(c => c.ContractNo == contractNo, ct);

    public void Add(Contract contract) => Db.Contracts.Add(contract);

    public void AddApprovalRequest(ApprovalRequest approval) => Db.ApprovalRequests.Add(approval);
}
