namespace QTKD.Api.Entities;

/// <summary>
/// Bản ghi mô phỏng "yêu cầu phê duyệt" sinh ra khi gửi duyệt thành công.
/// Chưa có luồng Approval thật; bảng này chỉ để chứng minh một hợp đồng chỉ được gửi duyệt một lần
/// (ràng buộc duy nhất trên ContractId).
/// </summary>
public class ApprovalRequest
{
    public int Id { get; set; }

    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;

    public DateTime SubmittedAtUtc { get; set; }
}
