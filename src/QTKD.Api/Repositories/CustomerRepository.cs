using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.Entities;

namespace QTKD.Api.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly QtkdDbContext _db;

    public CustomerRepository(QtkdDbContext db) => _db = db;

    // AsNoTracking: chỉ đọc, không cần EF theo dõi thay đổi -> nhẹ hơn.
    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct)
        => await _db.Customers.AsNoTracking().OrderBy(c => c.Id).ToListAsync(ct);
}
