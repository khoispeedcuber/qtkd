using QTKD.Api.DTOs;

namespace QTKD.Api.Services;

public interface ICustomerService
{
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct);

    /// <exception cref="Exceptions.RequestValidationException">Id không hợp lệ.</exception>
    /// <exception cref="Exceptions.NotFoundException">Không có khách hàng với Id này.</exception>
    Task<CustomerDto> GetByIdAsync(int id, CancellationToken ct);

    /// <exception cref="Exceptions.RequestValidationException">Thiếu hoặc sai thông tin bắt buộc.</exception>
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken ct);

    /// <exception cref="Exceptions.RequestValidationException">Id, tên hoặc trạng thái không hợp lệ.</exception>
    /// <exception cref="Exceptions.NotFoundException">Không có khách hàng với Id này.</exception>
    Task<CustomerDto> UpdateAsync(int id, UpdateCustomerRequest request, CancellationToken ct);
}
