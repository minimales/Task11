using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Services.Abstractions;

namespace task11.Web.Controllers;

/// <summary>
/// Financial operation endpoints. Wallet-scoped listing lives under the wallet route;
/// item operations live under <c>/api/operations</c>. All routes require authentication.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public sealed class OperationsController : ControllerBase
{
    private readonly IOperationService _operations;

    public OperationsController(IOperationService operations)
    {
        _operations = operations;
    }

    /// <summary>Lists the operations of a wallet the caller can access.</summary>
    [HttpGet("api/wallets/{walletId:guid}/operations")]
    [ProducesResponseType(typeof(IReadOnlyList<OperationModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OperationModel>>> GetByWallet(
        Guid walletId,
        CancellationToken cancellationToken)
    {
        var result = await _operations.GetByWalletAsync(walletId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns a single operation by id, ownership-checked via its wallet.</summary>
    [HttpGet("api/operations/{id:guid}")]
    [ProducesResponseType(typeof(OperationModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _operations.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>Creates an operation (converting the amount to the wallet base currency).</summary>
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
        var result = await _operations.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Updates an existing operation (re-converting the amount).</summary>
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
        var result = await _operations.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Soft-deletes an operation.</summary>
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
