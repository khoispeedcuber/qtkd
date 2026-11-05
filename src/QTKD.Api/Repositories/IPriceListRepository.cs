using QTKD.Api.Entities;

namespace QTKD.Api.Repositories;

public interface IPriceListRepository
{
    Task<IReadOnlyList<PriceList>> GetAllWithItemsAsync(CancellationToken ct);

    /// <summary>Lấy bảng giá kèm các dòng giá (chỉ đọc); null nếu không có.</summary>
    Task<PriceList?> GetByIdWithItemsAsync(int id, CancellationToken ct);

    Task<bool> ExistsAsync(int id, CancellationToken ct);

    Task<bool> ItemExistsAsync(int priceListId, string serviceCode, CancellationToken ct);

    void Add(PriceList priceList);

    void AddItem(PriceItem item);

    /// <exception cref="Exceptions.ConflictException">Trùng khóa (cùng mã dịch vụ trong cùng bảng giá).</exception>
    Task SaveChangesAsync(CancellationToken ct);
}
