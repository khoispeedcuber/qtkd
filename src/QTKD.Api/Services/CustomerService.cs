using QTKD.Api.DTOs;
using QTKD.Api.Repositories;

namespace QTKD.Api.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customers;

    public CustomerService(ICustomerRepository customers) => _customers = customers;

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct)
    {
        var customers = await _customers.GetAllAsync(ct);

        // LINQ Select: chuyển từng Entity thành DTO
        return customers
            .Select(c => new CustomerDto(c.Id, c.Name, c.Status.ToString()))
            .ToList();
    }
}
