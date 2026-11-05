using QTKD.Api.Entities;

namespace QTKD.Api.Repositories;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct);

    /// <summary>Lấy khách hàng theo Id (có theo dõi thay đổi, dùng được cho cả đọc và sửa); null nếu không có.</summary>
    Task<Customer?> GetByIdAsync(int id, CancellationToken ct);

    void Add(Customer customer);

    Task SaveChangesAsync(CancellationToken ct);
}
