using System.ComponentModel.DataAnnotations;

namespace QTKD.Api.DTOs;

/// <summary>
/// Sửa hợp đồng (thay thế toàn bộ các trường sửa được; số hợp đồng không đổi).
/// <see cref="Status"/> không dùng để đổi trạng thái: nếu gửi khác trạng thái hiện tại sẽ bị từ chối (CTR-03).
/// </summary>
public class UpdateContractRequest
{
    [Required(ErrorMessage = "Khách hàng (customerId) là bắt buộc.")]
    public int? CustomerId { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    [StringLength(4000, ErrorMessage = "Nội dung hợp đồng tối đa 4000 ký tự.")]
    public string? Content { get; set; }

    [StringLength(260, ErrorMessage = "Tên tệp đính kèm tối đa 260 ký tự.")]
    public string? AttachmentFileName { get; set; }

    public string? Status { get; set; }
}
