using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Services.Abstractions;

namespace task11.Web.Controllers;

/// <summary>
/// Wallet endpoints. All routes require an authenticated caller (global fallback policy);
/// access to individual wallets is isolated in the service layer.
/// </summary>
[ApiController]
[Route("api/wallets")]
[Produces("application/json")]
public sealed class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletsController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    /// <summary>Lists the caller's own personal wallets plus shared wallets.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WalletModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WalletModel>>> GetAll(CancellationToken cancellationToken)
    {
        var wallets = await _walletService.GetAccessibleAsync(cancellationToken);
        return Ok(wallets);
    }

    /// <summary>Returns a single accessible wallet by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WalletModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var wallet = await _walletService.GetByIdAsync(id, cancellationToken);
        return Ok(wallet);
    }

    /// <summary>Creates a personal wallet owned by the caller.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(WalletModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WalletModel>> Create(
        [FromBody] CreateWalletModel request,
        CancellationToken cancellationToken)
    {
        var wallet = await _walletService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = wallet.Id }, wallet);
    }

    /// <summary>Updates an accessible wallet.</summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(WalletModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WalletModel>> Update(
        Guid id,
        [FromBody] UpdateWalletModel request,
        CancellationToken cancellationToken)
    {
        var wallet = await _walletService.UpdateAsync(id, request, cancellationToken);
        return Ok(wallet);
    }

    /// <summary>Soft-deletes an accessible wallet.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _walletService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
