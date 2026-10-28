namespace QTKD.Api.DTOs;

/// <summary>Dữ liệu khách hàng trả ra cho client (không lộ trực tiếp Entity).</summary>
public record CustomerDto(int Id, string Name, string Status);
