using QTKD.Api.DTOs;
using QTKD.Api.Entities;
using QTKD.Api.Exceptions;
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
        return customers.Select(ToDto).ToList();
    }

    public async Task<CustomerDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var customer = await FindAsync(id, ct);
        return ToDto(customer);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct)
    {
        var name = RequireName(request.Name);

        var customer = new Customer { Name = name, Status = CustomerStatus.Active };
        _customers.Add(customer);
        await _customers.SaveChangesAsync(ct);

        return ToDto(customer);
    }

    public async Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken ct)
    {
        var customer = await FindAsync(id, ct);

        var name = RequireName(request.Name);
        if (!Enum.TryParse<CustomerStatus>(request.Status?.Trim(), ignoreCase: true, out var status)
            || !Enum.IsDefined(status))
            throw new RequestValidationException("Trạng thái khách hàng phải là Active hoặc Suspended.");

        customer.Name = name;
        customer.Status = status;
        await _customers.SaveChangesAsync(ct);

        return ToDto(customer);
    }

    private async Task<Customer> FindAsync(int id, CancellationToken ct)
    {
        if (id <= 0)
            throw new RequestValidationException("Id khách hàng phải là số nguyên dương.");

        return await _customers.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Khách hàng có Id = {id} không tồn tại.");
    }

    // Validation đặt ở Service để dùng được cho mọi nơi gọi, không chỉ Controller.
    private static string RequireName(string? name)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new RequestValidationException("Tên khách hàng là bắt buộc.");
        if (trimmed.Length > 200)
            throw new RequestValidationException("Tên khách hàng tối đa 200 ký tự.");
        return trimmed;
    }

    private static CustomerDto ToDto(Customer c) => new(c.Id, c.Name, c.Status.ToString());
}
