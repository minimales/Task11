using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Services.Abstractions;

namespace task11.Web.Controllers;

/// <summary>
/// Wallet-scoped operation types. Listing and creation are nested under a wallet;
/// retrieval, update and deletion address a type directly by id. Access to the owning
/// wallet is enforced in the service layer.
/// </summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public sealed class OperationTypesController : ControllerBase
{
    private readonly IOperationTypeService _service;

    public OperationTypesController(IOperationTypeService service)
    {
        _service = service;
    }

    /// <summary>Lists the operation types of a wallet.</summary>
    [HttpGet("api/wallets/{walletId:guid}/operation-types")]
    [ProducesResponseType(typeof(IReadOnlyList<OperationTypeModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<OperationTypeModel>>> GetByWallet(
        Guid walletId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByWalletAsync(walletId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Creates an operation type in a wallet.</summary>
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
        var created = await _service.CreateAsync(walletId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Returns a single operation type by id.</summary>
    [HttpGet("api/operation-types/{id:guid}")]
    [ProducesResponseType(typeof(OperationTypeModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationTypeModel>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>Updates an operation type.</summary>
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
        var updated = await _service.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Soft-deletes an operation type.</summary>
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
