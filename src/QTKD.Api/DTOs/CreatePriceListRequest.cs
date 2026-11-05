using System.ComponentModel.DataAnnotations;

namespace QTKD.Api.DTOs;

public class CreatePriceListRequest
{
    [Required(ErrorMessage = "Tên bảng giá là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Tên bảng giá tối đa 200 ký tự.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Ngày hiệu lực (effectiveDate) là bắt buộc.")]
    public DateOnly? EffectiveDate { get; set; }
}
