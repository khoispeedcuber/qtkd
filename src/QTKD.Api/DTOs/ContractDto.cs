namespace QTKD.Api.DTOs;

/// <summary>Dữ liệu hợp đồng trả ra cho client, kèm tên khách hàng.</summary>
public record ContractDto(
    int Id,
    string ContractNo,
    int CustomerId,
    string CustomerName,
    DateOnly StartDate,
    DateOnly EndDate,
    string Status);
