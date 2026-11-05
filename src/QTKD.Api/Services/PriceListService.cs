using QTKD.Api.DTOs;
using QTKD.Api.Entities;
using QTKD.Api.Exceptions;
using QTKD.Api.Repositories;

namespace QTKD.Api.Services;

public class PriceListService : IPriceListService
{
    private readonly IPriceListRepository _priceLists;

    public PriceListService(IPriceListRepository priceLists) => _priceLists = priceLists;

    public async Task<IReadOnlyList<PriceListDto>> GetAllAsync(CancellationToken ct)
    {
        var lists = await _priceLists.GetAllWithItemsAsync(ct);
        return lists.Select(ToDto).ToList();
    }

    public async Task<PriceListDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var list = await FindWithItemsAsync(id, ct);
        return ToDto(list);
    }

    public async Task<PriceListDto> CreateAsync(CreatePriceListRequest request, CancellationToken ct)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            throw new RequestValidationException("Tên bảng giá là bắt buộc.");
        if (request.EffectiveDate is null)
            throw new RequestValidationException("Ngày hiệu lực (effectiveDate) là bắt buộc.");

        var list = new PriceList { Name = name, EffectiveDate = request.EffectiveDate.Value };
        _priceLists.Add(list);
        await _priceLists.SaveChangesAsync(ct);

        return ToDto(list);
    }

    public async Task<IReadOnlyList<PriceItemDto>> GetItemsAsync(int priceListId, CancellationToken ct)
    {
        var list = await FindWithItemsAsync(priceListId, ct);
        return ToDto(list).Items;
    }

    public async Task<PriceItemDto> AddItemAsync(int priceListId, CreatePriceItemRequest request, CancellationToken ct)
    {
        // Dòng giá phải gắn đúng một bảng giá có thật.
        if (priceListId <= 0 || !await _priceLists.ExistsAsync(priceListId, ct))
            throw new NotFoundException($"Bảng giá có Id = {priceListId} không tồn tại.");

        var serviceCode = request.ServiceCode?.Trim();
        if (string.IsNullOrEmpty(serviceCode))
            throw new RequestValidationException("Mã dịch vụ (serviceCode) là bắt buộc.");
        if (request.UnitPrice is null || request.UnitPrice < 0)
            throw new RequestValidationException("Đơn giá phải lớn hơn hoặc bằng 0.");

        if (await _priceLists.ItemExistsAsync(priceListId, serviceCode, ct))
            throw new ConflictException($"Mã dịch vụ '{serviceCode}' đã có trong bảng giá {priceListId}.");

        var item = new PriceItem
        {
            PriceListId = priceListId,
            ServiceCode = serviceCode,
            UnitPrice = request.UnitPrice.Value
        };
        _priceLists.AddItem(item);
        await _priceLists.SaveChangesAsync(ct);

        return new PriceItemDto(item.PriceListId, item.ServiceCode, item.UnitPrice);
    }

    private async Task<PriceList> FindWithItemsAsync(int id, CancellationToken ct)
    {
        if (id <= 0)
            throw new RequestValidationException("Id bảng giá phải là số nguyên dương.");

        return await _priceLists.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException($"Bảng giá có Id = {id} không tồn tại.");
    }

    private static PriceListDto ToDto(PriceList p) => new(
        p.Id,
        p.Name,
        p.EffectiveDate,
        p.Items.OrderBy(i => i.ServiceCode)
               .Select(i => new PriceItemDto(i.PriceListId, i.ServiceCode, i.UnitPrice))
               .ToList());
}
