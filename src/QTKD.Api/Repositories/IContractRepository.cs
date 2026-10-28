using QTKD.Api.Entities;

namespace QTKD.Api.Repositories;

public interface IContractRepository
{
    /// <summary>Lấy hợp đồng theo Id kèm Customer; trả null nếu không có.</summary>
    Task<Contract?> GetByIdWithCustomerAsync(int id, CancellationToken ct);
}
