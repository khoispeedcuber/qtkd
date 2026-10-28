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

    // Không dùng ràng buộc {id:int}: id sai kiểu (ví dụ "abc") sẽ được [ApiController]
    // tự trả 400 kèm thông báo lỗi, thay vì 404 rỗng khó hiểu cho client.
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContractDto>> GetById(int id, CancellationToken ct)
        => Ok(await _contracts.GetByIdAsync(id, ct));
}
