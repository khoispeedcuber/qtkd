using Microsoft.EntityFrameworkCore;
using QTKD.Api.Data;
using QTKD.Api.Entities;

namespace QTKD.Api.Repositories;

public class CustomerRepository : RepositoryBase, ICustomerRepository
{
    public CustomerRepository(QtkdDbContext db) : base(db) { }

    // AsNoTracking: chỉ đọc, không cần EF theo dõi thay đổi -> nhẹ hơn.
    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct)
        => await Db.Customers.AsNoTracking().OrderBy(c => c.Id).ToListAsync(ct);

    public Task<Customer?> GetByIdAsync(int id, CancellationToken ct)
        => Db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);

    public void Add(Customer customer) => Db.Customers.Add(customer);
}
