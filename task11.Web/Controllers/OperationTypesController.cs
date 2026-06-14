using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Services.Abstractions;

namespace task11.Web.Controllers;

[ApiController]
[Authorize]
public class OperationTypesController : ControllerBase
{
    private readonly IOperationTypeService _service;

    public OperationTypesController(IOperationTypeService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        _service = service;
    }

    [HttpGet("api/wallets/{walletId:guid}/operation-types")]
    [ProducesResponseType(typeof(IReadOnlyList<OperationTypeModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OperationTypeModel>>> GetByWallet(
        Guid walletId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationTypeModel> result = await _service.GetByWalletAsync(walletId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/wallets/{walletId:guid}/operation-types")]
    [ProducesResponseType(typeof(OperationTypeModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OperationTypeModel>> Create(
        Guid walletId,
        [FromBody] CreateOperationTypeModel request,
        CancellationToken cancellationToken)
    {
        OperationTypeModel created = await _service.CreateAsync(walletId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("api/operation-types/{id:guid}")]
    [ProducesResponseType(typeof(OperationTypeModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationTypeModel>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        OperationTypeModel result = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("api/operation-types/{id:guid}")]
    [ProducesResponseType(typeof(OperationTypeModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OperationTypeModel>> Update(
        Guid id,
        [FromBody] UpdateOperationTypeModel request,
        CancellationToken cancellationToken)
    {
        OperationTypeModel updated = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("api/operation-types/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
