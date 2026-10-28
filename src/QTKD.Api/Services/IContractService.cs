using QTKD.Api.DTOs;

namespace QTKD.Api.Services;

public interface IContractService
{
    /// <exception cref="Exceptions.RequestValidationException">Id không hợp lệ.</exception>
    /// <exception cref="Exceptions.NotFoundException">Không có hợp đồng với Id này.</exception>
    Task<ContractDto> GetByIdAsync(int id, CancellationToken ct);
}
