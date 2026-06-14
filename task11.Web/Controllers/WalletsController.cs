using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Services.Abstractions;

namespace task11.Web.Controllers;

[ApiController]
[Route("api/wallets")]
[Produces("application/json")]
public class WalletsController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletsController(IWalletService walletService)
    {
        ArgumentNullException.ThrowIfNull(walletService);

        _walletService = walletService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WalletModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WalletModel>>> GetAll(CancellationToken cancellationToken)
    {
        var wallets = await _walletService.GetAccessibleAsync(cancellationToken);
        return Ok(wallets);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WalletModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WalletModel>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var wallet = await _walletService.GetByIdAsync(id, cancellationToken);
        return Ok(wallet);
    }

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
