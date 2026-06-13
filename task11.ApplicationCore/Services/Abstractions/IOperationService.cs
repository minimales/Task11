using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Services.Abstractions;

/// <summary>
/// Application service for financial operations. Enforces wallet ownership isolation,
/// performs currency conversion to the wallet base currency, and applies soft deletes.
/// </summary>
public interface IOperationService
{
    /// <summary>Lists the operations of an accessible wallet (newest first).</summary>
    /// <exception cref="NotFoundException">Wallet does not exist.</exception>
    /// <exception cref="ForbiddenException">Wallet not accessible to the caller.</exception>
    Task<IReadOnlyList<OperationModel>> GetByWalletAsync(Guid walletId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single operation by id, ownership-checked via its wallet.</summary>
    /// <exception cref="NotFoundException">Operation does not exist.</exception>
    /// <exception cref="ForbiddenException">Operation's wallet not accessible.</exception>
    Task<OperationModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an operation. The amount is converted to the wallet base currency when a
    /// differing transaction currency is supplied, and an audit string is appended to the note.
    /// </summary>
    /// <exception cref="NotFoundException">Wallet or type does not exist.</exception>
    /// <exception cref="ForbiddenException">Wallet not accessible.</exception>
    /// <exception cref="FxUnavailableException">FX rate unavailable.</exception>
    Task<OperationModel> CreateAsync(CreateOperationModel request, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing operation, re-converting the amount to the wallet base currency.</summary>
    /// <exception cref="NotFoundException">Operation or type does not exist.</exception>
    /// <exception cref="ForbiddenException">Operation's wallet not accessible.</exception>
    /// <exception cref="FxUnavailableException">FX rate unavailable.</exception>
    Task<OperationModel> UpdateAsync(Guid id, UpdateOperationModel request, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes an operation after an ownership check.</summary>
    /// <exception cref="NotFoundException">Operation does not exist.</exception>
    /// <exception cref="ForbiddenException">Operation's wallet not accessible.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
