namespace QTKD.Api.Entities;

public class Contract
{
    public int Id { get; set; }
    public string ContractNo { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Nullable: hợp đồng Draft được phép lưu khi chưa đủ thông tin; CTR-02 kiểm tra đủ hay chưa lúc gửi duyệt.
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>Nội dung hợp đồng (điều kiện bắt buộc khi gửi duyệt - xem docs/doi-chieu-quy-tac.md).</summary>
    public string? Content { get; set; }

    /// <summary>Tên tệp đính kèm. Chỉ lưu tên, hệ thống chưa upload/lưu tệp.</summary>
    public string? AttachmentFileName { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Draft;
}
