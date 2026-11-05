using Microsoft.AspNetCore.Mvc;
using QTKD.Api.DTOs;
using QTKD.Api.Services;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/price-lists")]
public class PriceListsController : ControllerBase
{
    private readonly IPriceListService _priceLists;

    public PriceListsController(IPriceListService priceLists) => _priceLists = priceLists;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PriceListDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PriceListDto>>> GetAll(CancellationToken ct)
        => Ok(await _priceLists.GetAllAsync(ct));

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListDto>> GetById(int id, CancellationToken ct)
        => Ok(await _priceLists.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType(typeof(PriceListDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PriceListDto>> Create(CreatePriceListRequest request, CancellationToken ct)
    {
        var created = await _priceLists.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id}/items")]
    [ProducesResponseType(typeof(IReadOnlyList<PriceItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PriceItemDto>>> GetItems(int id, CancellationToken ct)
        => Ok(await _priceLists.GetItemsAsync(id, ct));

    [HttpPost("{id}/items")]
    [ProducesResponseType(typeof(PriceItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PriceItemDto>> AddItem(int id, CreatePriceItemRequest request, CancellationToken ct)
    {
        var created = await _priceLists.AddItemAsync(id, request, ct);
        return CreatedAtAction(nameof(GetItems), new { id }, created);
    }
}
