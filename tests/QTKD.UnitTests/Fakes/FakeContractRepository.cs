using QTKD.Api.Entities;
using QTKD.Api.Repositories;

namespace QTKD.UnitTests.Fakes;

public class FakeContractRepository : IContractRepository
{
    public List<Contract> Contracts { get; } = new();
    public List<ApprovalRequest> ApprovalRequests { get; } = new();
    public int SaveChangesCount { get; private set; }

    public Task<IReadOnlyList<Contract>> GetAllWithCustomerAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Contract>>(Contracts.ToList());

    public Task<Contract?> GetByIdWithCustomerAsync(int id, CancellationToken ct)
        => Task.FromResult(Contracts.FirstOrDefault(c => c.Id == id));

    public Task<Contract?> GetForUpdateAsync(int id, CancellationToken ct)
        => Task.FromResult(Contracts.FirstOrDefault(c => c.Id == id));

    public Task<bool> ContractNoExistsAsync(string contractNo, CancellationToken ct)
        => Task.FromResult(Contracts.Any(c => c.ContractNo == contractNo));

    public void Add(Contract contract) => Contracts.Add(contract);

    public void AddApprovalRequest(ApprovalRequest approval) => ApprovalRequests.Add(approval);

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveChangesCount++;
        return Task.CompletedTask;
    }
}
