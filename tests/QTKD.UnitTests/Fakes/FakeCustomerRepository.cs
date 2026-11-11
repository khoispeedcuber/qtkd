using QTKD.Api.Entities;
using QTKD.Api.Repositories;

namespace QTKD.UnitTests.Fakes;

public class FakeCustomerRepository : ICustomerRepository
{
    public List<Customer> Customers { get; } = new();
    public int SaveChangesCount { get; private set; }

    public Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Customer>>(Customers.ToList());

    public Task<Customer?> GetByIdAsync(int id, CancellationToken ct)
        => Task.FromResult(Customers.FirstOrDefault(c => c.Id == id));

    public void Add(Customer customer) => Customers.Add(customer);

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveChangesCount++;
        return Task.CompletedTask;
    }
}
