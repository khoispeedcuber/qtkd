namespace QTKD.Api.DTOs;

/// <summary>Kết quả gửi duyệt thành công (POST /api/contracts/{id}/submit).</summary>
public record SubmitContractResponse(int ContractId, string Status, int ApprovalId);
