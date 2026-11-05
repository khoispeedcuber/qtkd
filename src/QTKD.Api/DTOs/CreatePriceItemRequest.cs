using System.ComponentModel.DataAnnotations;

namespace QTKD.Api.DTOs;

public class CreatePriceItemRequest
{
    [Required(ErrorMessage = "Mã dịch vụ (serviceCode) là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Mã dịch vụ tối đa 50 ký tự.")]
    public string? ServiceCode { get; set; }

    [Required(ErrorMessage = "Đơn giá (unitPrice) là bắt buộc.")]
    [Range(0, 9999999999999999.0, ErrorMessage = "Đơn giá phải lớn hơn hoặc bằng 0.")]
    public decimal? UnitPrice { get; set; }
}
