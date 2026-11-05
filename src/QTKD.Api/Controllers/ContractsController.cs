using Microsoft.AspNetCore.Mvc;
using QTKD.Api.DTOs;
using QTKD.Api.Services;

namespace QTKD.Api.Controllers;

[ApiController]
[Route("api/contracts")]
public class ContractsController : ControllerBase
{
    private readonly IContractService _contracts;

    public ContractsController(IContractService contracts) => _contracts = contracts;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ContractDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ContractDto>>> GetAll(CancellationToken ct)
        => Ok(await _contracts.GetAllAsync(ct));

    // Không dùng ràng buộc {id:int}: id sai kiểu (ví dụ "abc") sẽ được [ApiController]
    // tự trả 400 kèm thông báo lỗi, thay vì 404 rỗng khó hiểu cho client.
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContractDto>> GetById(int id, CancellationToken ct)
        => Ok(await _contracts.GetByIdAsync(id, ct));

    [HttpPost]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContractDto>> Create(CreateContractRequest request, CancellationToken ct)
    {
        var created = await _contracts.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Sửa hợp đồng. CTR-01: chỉ khi Draft/RevisionRequested. CTR-03: không đổi trạng thái qua đây.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContractDto>> Update(int id, UpdateContractRequest request, CancellationToken ct)
        => Ok(await _contracts.UpdateAsync(id, request, ct));

    /// <summary>US03 - Gửi duyệt: Draft => PendingApproval khi đủ điều kiện CTR-02.</summary>
    [HttpPost("{id}/submit")]
    [ProducesResponseType(typeof(SubmitContractResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmitContractResponse>> Submit(int id, CancellationToken ct)
        => Ok(await _contracts.SubmitAsync(id, ct));
}
