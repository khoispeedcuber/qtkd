using System.ComponentModel.DataAnnotations;

namespace QTKD.Api.DTOs;

/// <summary>Tạo hợp đồng. Hợp đồng mới luôn ở trạng thái Draft (không nhận trạng thái từ client).</summary>
public class CreateContractRequest
{
    [Required(ErrorMessage = "Số hợp đồng là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Số hợp đồng tối đa 50 ký tự.")]
    public string? ContractNo { get; set; }

    [Required(ErrorMessage = "Khách hàng (customerId) là bắt buộc.")]
    public int? CustomerId { get; set; }

    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    [StringLength(4000, ErrorMessage = "Nội dung hợp đồng tối đa 4000 ký tự.")]
    public string? Content { get; set; }

    [StringLength(260, ErrorMessage = "Tên tệp đính kèm tối đa 260 ký tự.")]
    public string? AttachmentFileName { get; set; }
}
