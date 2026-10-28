namespace QTKD.Api.Entities;

public enum CustomerStatus
{
    Active,
    Suspended
}

// Theo đề Phase 2 (PendingApproval). Sơ đồ 5.1 của đề gốc dùng Submitted/Under Review:
// nếu nhóm đã chốt tên khác, đổi ở đây và ghi lý do vào docs/architecture.md.
// Tuần 05 chỉ ĐỌC trạng thái; việc chuyển trạng thái (CTR-01/02/03) thuộc tuần 06.
public enum ContractStatus
{
    Draft,
    PendingApproval,
    RevisionRequested,
    Approved,
    Active,
    Rejected,
    Cancelled,
    Expired
}
