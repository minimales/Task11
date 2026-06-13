using task11.ApplicationCore.Models;
using task11.Data.Entities;

namespace task11.ApplicationCore.Services.Abstractions;

/// <summary>
/// Wallet use-cases with ownership isolation. Also exposes
/// <see cref="EnsureCanAccessAsync"/> so OperationType/Operation/Report services
/// can reuse the same access rule for resolving a target wallet.
/// </summary>
public interface IWalletService
{
    /// <summary>
    /// Lists wallets accessible to the current user: own personal wallets plus shared wallets
    /// (admins see all).
    /// </summary>
    Task<IReadOnlyList<WalletModel>> GetAccessibleAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a single accessible wallet by id.</summary>
    /// <exception cref="NotFoundException">No such wallet.</exception>
    /// <exception cref="ForbiddenException">Not accessible to the caller.</exception>
    Task<WalletModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates a personal wallet owned by the current user.</summary>
    Task<WalletModel> CreateAsync(CreateWalletModel request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an accessible wallet. The base currency is immutable once the wallet has operations.
    /// </summary>
    /// <exception cref="NotFoundException">No such wallet.</exception>
    /// <exception cref="ForbiddenException">Not accessible to the caller.</exception>
    /// <exception cref="ConflictException">Base currency change attempted while operations exist.</exception>
    Task<WalletModel> UpdateAsync(Guid id, UpdateWalletModel request, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes an accessible wallet.</summary>
    /// <exception cref="NotFoundException">No such wallet.</exception>
    /// <exception cref="ForbiddenException">Not accessible to the caller.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the wallet by id and verifies the current user may access it
    /// (shared, owned, or admin), returning the entity. Reused by other modules
    /// to enforce isolation before touching wallet-scoped data.
    /// </summary>
    /// <exception cref="NotFoundException">No such wallet.</exception>
    /// <exception cref="ForbiddenException">Not accessible to the caller.</exception>
    Task<WalletEntity> EnsureCanAccessAsync(Guid walletId, CancellationToken cancellationToken = default);
}
