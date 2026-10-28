using QTKD.Api.Entities;

namespace QTKD.Api.Repositories;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct);
}
