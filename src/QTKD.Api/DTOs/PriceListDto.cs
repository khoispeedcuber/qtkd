namespace QTKD.Api.DTOs;

public record PriceItemDto(int PriceListId, string ServiceCode, decimal UnitPrice);

public record PriceListDto(int Id, string Name, DateOnly EffectiveDate, IReadOnlyList<PriceItemDto> Items);
