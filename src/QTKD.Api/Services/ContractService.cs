using QTKD.Api.DTOs;
using QTKD.Api.Exceptions;
using QTKD.Api.Repositories;

namespace QTKD.Api.Services;

public class ContractService : IContractService
{
    private readonly IContractRepository _contracts;

    public ContractService(IContractRepository contracts) => _contracts = contracts;

    public async Task<ContractDto> GetByIdAsync(int id, CancellationToken ct)
    {
        // Validation đầu vào đặt ở Service để dùng được cho mọi nơi gọi, không chỉ Controller.
        if (id <= 0)
            throw new RequestValidationException("Id hợp đồng phải là số nguyên dương.");

        var contract = await _contracts.GetByIdWithCustomerAsync(id, ct)
            ?? throw new NotFoundException($"Hợp đồng có Id = {id} không tồn tại.");

        return new ContractDto(
            contract.Id,
            contract.ContractNo,
            contract.CustomerId,
            contract.Customer.Name,
            contract.StartDate,
            contract.EndDate,
            contract.Status.ToString());
    }
}
