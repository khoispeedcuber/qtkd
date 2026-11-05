using System.ComponentModel.DataAnnotations;

namespace QTKD.Api.DTOs;

public class UpdateCustomerRequest
{
    [Required(ErrorMessage = "Tên khách hàng là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Tên khách hàng tối đa 200 ký tự.")]
    public string? Name { get; set; }

    /// <summary>Active hoặc Suspended.</summary>
    [Required(ErrorMessage = "Trạng thái khách hàng là bắt buộc (Active hoặc Suspended).")]
    public string? Status { get; set; }
}
