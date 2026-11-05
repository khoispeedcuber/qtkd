using QTKD.Api.DTOs;

namespace QTKD.Api.Services;

public interface IContractService
{
    Task<IReadOnlyList<ContractDto>> GetAllAsync(CancellationToken ct);

    /// <exception cref="Exceptions.RequestValidationException">Id không hợp lệ.</exception>
    /// <exception cref="Exceptions.NotFoundException">Không có hợp đồng với Id này.</exception>
    Task<ContractDto> GetByIdAsync(int id, CancellationToken ct);

    /// <exception cref="Exceptions.RequestValidationException">Thiếu/sai dữ liệu, khách hàng không hợp lệ, ngày không hợp lệ.</exception>
    /// <exception cref="Exceptions.ConflictException">Số hợp đồng đã tồn tại.</exception>
    Task<ContractDto> CreateAsync(CreateContractRequest request, CancellationToken ct);

    /// <exception cref="Exceptions.NotFoundException">Không có hợp đồng với Id này.</exception>
    /// <exception cref="Exceptions.ConflictException">CTR-01: hợp đồng không ở Draft/RevisionRequested.</exception>
    /// <exception cref="Exceptions.RuleViolationException">CTR-03: yêu cầu đổi trạng thái sang Approved.</exception>
    /// <exception cref="Exceptions.RequestValidationException">Dữ liệu hoặc khách hàng không hợp lệ.</exception>
    Task<ContractDto> UpdateAsync(int id, UpdateContractRequest request, CancellationToken ct);

    /// <exception cref="Exceptions.NotFoundException">Không có hợp đồng với Id này.</exception>
    /// <exception cref="Exceptions.ConflictException">Hợp đồng không ở Draft (kể cả gửi lặp lại).</exception>
    /// <exception cref="Exceptions.RuleViolationException">CTR-02: thiếu điều kiện gửi duyệt.</exception>
    Task<SubmitContractResponse> SubmitAsync(int id, CancellationToken ct);
}
