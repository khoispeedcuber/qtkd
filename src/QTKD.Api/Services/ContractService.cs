using QTKD.Api.DTOs;
using QTKD.Api.Entities;
using QTKD.Api.Exceptions;
using QTKD.Api.Repositories;

namespace QTKD.Api.Services;

public class ContractService : IContractService
{
    private readonly IContractRepository _contracts;
    private readonly ICustomerRepository _customers;

    public ContractService(IContractRepository contracts, ICustomerRepository customers)
    {
        _contracts = contracts;
        _customers = customers;
    }

    public async Task<IReadOnlyList<ContractDto>> GetAllAsync(CancellationToken ct)
    {
        var contracts = await _contracts.GetAllWithCustomerAsync(ct);
        return contracts.Select(ToDto).ToList();
    }

    public async Task<ContractDto> GetByIdAsync(int id, CancellationToken ct)
    {
        // Validation đầu vào đặt ở Service để dùng được cho mọi nơi gọi, không chỉ Controller.
        ValidateId(id);

        var contract = await _contracts.GetByIdWithCustomerAsync(id, ct)
            ?? throw new NotFoundException($"Hợp đồng có Id = {id} không tồn tại.");

        return ToDto(contract);
    }

    public async Task<ContractDto> CreateAsync(CreateContractRequest request, CancellationToken ct)
    {
        var contractNo = request.ContractNo?.Trim();
        if (string.IsNullOrEmpty(contractNo))
            throw new RequestValidationException("Số hợp đồng là bắt buộc.");
        if (request.CustomerId is null)
            throw new RequestValidationException("Khách hàng (customerId) là bắt buộc.");

        ValidateDateOrder(request.StartDate, request.EndDate);
        var customer = await RequireValidCustomerAsync(request.CustomerId.Value, ct);

        if (await _contracts.ContractNoExistsAsync(contractNo, ct))
            throw new ConflictException($"Số hợp đồng '{contractNo}' đã tồn tại.");

        var contract = new Contract
        {
            ContractNo = contractNo,
            Customer = customer,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Content = NullIfBlank(request.Content),
            AttachmentFileName = NullIfBlank(request.AttachmentFileName),
            Status = ContractStatus.Draft          // hợp đồng mới luôn là Draft
        };

        _contracts.Add(contract);
        await _contracts.SaveChangesAsync(ct);

        return ToDto(contract);
    }

    public async Task<ContractDto> UpdateAsync(int id, UpdateContractRequest request, CancellationToken ct)
    {
        ValidateId(id);

        var contract = await _contracts.GetForUpdateAsync(id, ct)
            ?? throw new NotFoundException($"Hợp đồng có Id = {id} không tồn tại.");

        // CTR-01: chỉ được sửa khi hợp đồng ở Draft hoặc RevisionRequested.
        if (contract.Status is not (ContractStatus.Draft or ContractStatus.RevisionRequested))
            throw new ConflictException(
                $"Chỉ được sửa hợp đồng ở trạng thái Draft hoặc RevisionRequested; hợp đồng hiện ở {contract.Status}.",
                "CTR-01");

        // CTR-03: không đổi trạng thái qua cập nhật, đặc biệt không được chuyển thẳng sang Approved.
        RejectStatusChange(contract, request.Status);

        if (request.CustomerId is null)
            throw new RequestValidationException("Khách hàng (customerId) là bắt buộc.");

        ValidateDateOrder(request.StartDate, request.EndDate);
        var customer = await RequireValidCustomerAsync(request.CustomerId.Value, ct);

        contract.Customer = customer;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.Content = NullIfBlank(request.Content);
        contract.AttachmentFileName = NullIfBlank(request.AttachmentFileName);
        await _contracts.SaveChangesAsync(ct);

        return ToDto(contract);
    }

    public async Task<SubmitContractResponse> SubmitAsync(int id, CancellationToken ct)
    {
        ValidateId(id);

        // Bước 1: tìm hợp đồng.
        var contract = await _contracts.GetForUpdateAsync(id, ct)
            ?? throw new NotFoundException($"Hợp đồng có Id = {id} không tồn tại.");

        // Bước 2: chỉ hợp đồng Draft mới được gửi duyệt. Gửi lại hợp đồng đã PendingApproval sẽ dừng ở đây,
        // không tạo thêm ApprovalRequest nào.
        if (contract.Status != ContractStatus.Draft)
            throw new ConflictException(
                $"Chỉ hợp đồng ở trạng thái Draft mới được gửi duyệt; hợp đồng hiện ở {contract.Status}.",
                "US03");

        // Bước 3 - CTR-02: gom TẤT CẢ điều kiện còn thiếu để báo một lần; chưa sửa gì trên hợp đồng.
        var errors = CollectSubmitErrors(contract);
        if (errors.Count > 0)
            throw new RuleViolationException(
                "CTR-02",
                "Hợp đồng chưa đủ điều kiện gửi duyệt. Hợp đồng vẫn ở trạng thái Draft.",
                errors);

        // Bước 4: hợp lệ -> chuyển Draft => PendingApproval (CTR-03: không bao giờ sang Approved ở đây)
        // và ghi ApprovalRequest mô phỏng; cả hai lưu trong cùng một lần SaveChanges (một transaction).
        contract.Status = ContractStatus.PendingApproval;
        var approval = new ApprovalRequest { Contract = contract, SubmittedAtUtc = DateTime.UtcNow };
        _contracts.AddApprovalRequest(approval);
        await _contracts.SaveChangesAsync(ct);

        return new SubmitContractResponse(contract.Id, contract.Status.ToString(), approval.Id);
    }

    // ---- CTR-02: điều kiện bắt buộc để gửi duyệt ----
    private static Dictionary<string, string[]> CollectSubmitErrors(Contract c)
    {
        var errors = new Dictionary<string, string[]>();

        if (c.Customer.Status != CustomerStatus.Active)
            errors["CustomerId"] = [$"Khách hàng '{c.Customer.Name}' không ở trạng thái Active."];

        if (c.StartDate is null)
            errors["StartDate"] = ["Thiếu ngày bắt đầu hiệu lực."];
        if (c.EndDate is null)
            errors["EndDate"] = ["Thiếu ngày kết thúc hiệu lực."];
        if (c.StartDate is not null && c.EndDate is not null && c.StartDate > c.EndDate)
            errors["EndDate"] = ["Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu."];

        if (string.IsNullOrWhiteSpace(c.Content))
            errors["Content"] = ["Thiếu nội dung hợp đồng."];
        if (string.IsNullOrWhiteSpace(c.AttachmentFileName))
            errors["AttachmentFileName"] = ["Thiếu tệp đính kèm."];

        return errors;
    }

    // ---- CTR-03 ----
    private static void RejectStatusChange(Contract contract, string? requestedStatus)
    {
        var requested = requestedStatus?.Trim();
        if (string.IsNullOrEmpty(requested)
            || requested.Equals(contract.Status.ToString(), StringComparison.OrdinalIgnoreCase))
            return;

        if (requested.Equals(nameof(ContractStatus.Approved), StringComparison.OrdinalIgnoreCase))
            throw new RuleViolationException(
                "CTR-03",
                $"Không được chuyển trực tiếp từ {contract.Status} sang Approved. " +
                "Hợp đồng phải được gửi duyệt (PendingApproval) trước.");

        throw new RequestValidationException(
            "Không đổi trạng thái hợp đồng qua cập nhật. Dùng POST /api/contracts/{id}/submit để gửi duyệt.");
    }

    // ---- Hàm dùng chung ----
    private static void ValidateId(int id)
    {
        if (id <= 0)
            throw new RequestValidationException("Id hợp đồng phải là số nguyên dương.");
    }

    private static void ValidateDateOrder(DateOnly? start, DateOnly? end)
    {
        if (start is not null && end is not null && start > end)
            throw new RequestValidationException("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
    }

    /// <summary>Khách hàng được tham chiếu phải tồn tại và đang Active.</summary>
    private async Task<Customer> RequireValidCustomerAsync(int customerId, CancellationToken ct)
    {
        var customer = await _customers.GetByIdAsync(customerId, ct)
            ?? throw new RequestValidationException($"Khách hàng có Id = {customerId} không tồn tại.");

        if (customer.Status != CustomerStatus.Active)
            throw new RequestValidationException($"Khách hàng '{customer.Name}' không ở trạng thái Active.");

        return customer;
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ContractDto ToDto(Contract c) => new(
        c.Id,
        c.ContractNo,
        c.CustomerId,
        c.Customer.Name,
        c.StartDate,
        c.EndDate,
        c.Content,
        c.AttachmentFileName,
        c.Status.ToString());
}
