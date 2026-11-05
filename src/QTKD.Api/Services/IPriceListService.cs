using QTKD.Api.DTOs;

namespace QTKD.Api.Services;

public interface IPriceListService
{
    Task<IReadOnlyList<PriceListDto>> GetAllAsync(CancellationToken ct);

    /// <exception cref="Exceptions.NotFoundException">Không có bảng giá với Id này.</exception>
    Task<PriceListDto> GetByIdAsync(int id, CancellationToken ct);

    Task<PriceListDto> CreateAsync(CreatePriceListRequest request, CancellationToken ct);

    /// <exception cref="Exceptions.NotFoundException">Không có bảng giá với Id này.</exception>
    Task<IReadOnlyList<PriceItemDto>> GetItemsAsync(int priceListId, CancellationToken ct);

    /// <exception cref="Exceptions.NotFoundException">Không có bảng giá với Id này.</exception>
    /// <exception cref="Exceptions.RequestValidationException">Mã dịch vụ hoặc đơn giá không hợp lệ.</exception>
    /// <exception cref="Exceptions.ConflictException">Mã dịch vụ đã có trong bảng giá này.</exception>
    Task<PriceItemDto> AddItemAsync(int priceListId, CreatePriceItemRequest request, CancellationToken ct);
}
