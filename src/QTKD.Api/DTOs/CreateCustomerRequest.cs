using System.ComponentModel.DataAnnotations;

namespace QTKD.Api.DTOs;

public class CreateCustomerRequest
{
    [Required(ErrorMessage = "Tên khách hàng là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Tên khách hàng tối đa 200 ký tự.")]
    public string? Name { get; set; }
}
