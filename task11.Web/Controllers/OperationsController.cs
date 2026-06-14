using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Services.Abstractions;

namespace task11.Web.Controllers;

[ApiController]
[Authorize]
public class OperationsController : ControllerBase
{
    private readonly IOperationService _operations;

    public OperationsController(IOperationService operations)
    {
        ArgumentNullException.ThrowIfNull(operations);

        _operations = operations;
    }

    [HttpGet("api/wallets/{walletId:guid}/operations")]
    [ProducesResponseType(typeof(IReadOnlyList<OperationModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OperationModel>>> GetByWallet(
        Guid walletId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationModel> result = await _operations.GetByWalletAsync(walletId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/operations/{id:guid}")]
    [ProducesResponseType(typeof(OperationModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        OperationModel result = await _operations.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/operations")]
    [ProducesResponseType(typeof(OperationModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<OperationModel>> Create(
        [FromBody] CreateOperationModel request,
        CancellationToken cancellationToken)
    {
        OperationModel result = await _operations.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("api/operations/{id:guid}")]
    [ProducesResponseType(typeof(OperationModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<OperationModel>> Update(
        Guid id,
        [FromBody] UpdateOperationModel request,
        CancellationToken cancellationToken)
    {
        OperationModel result = await _operations.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/operations/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _operations.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
