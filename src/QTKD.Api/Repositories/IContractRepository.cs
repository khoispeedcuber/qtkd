using QTKD.Api.Entities;

namespace QTKD.Api.Repositories;

public interface IContractRepository
{
    Task<IReadOnlyList<Contract>> GetAllWithCustomerAsync(CancellationToken ct);

    /// <summary>Lấy hợp đồng theo Id kèm Customer (chỉ đọc); trả null nếu không có.</summary>
    Task<Contract?> GetByIdWithCustomerAsync(int id, CancellationToken ct);

    /// <summary>Lấy hợp đồng theo Id kèm Customer, có theo dõi thay đổi để sửa/gửi duyệt; null nếu không có.</summary>
    Task<Contract?> GetForUpdateAsync(int id, CancellationToken ct);

    Task<bool> ContractNoExistsAsync(string contractNo, CancellationToken ct);

    void Add(Contract contract);

    void AddApprovalRequest(ApprovalRequest approval);

    /// <exception cref="Exceptions.ConflictException">Vi phạm ràng buộc duy nhất (trùng số hợp đồng, gửi duyệt trùng).</exception>
    Task SaveChangesAsync(CancellationToken ct);
}
