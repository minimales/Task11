using task11.Data.Entities;

namespace task11.ApplicationCore.Repositories.Abstractions;

/// <summary>
/// Data-access contract for <see cref="WalletEntity"/>. Adds the wallet-scoped reads used for
/// ownership isolation and the immutable-currency rule. Each method opens its own context.
/// </summary>
public interface IWalletRepository
{
    /// <summary>Returns a wallet by id, or null when not found / soft-deleted.</summary>
    Task<WalletEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists wallets accessible to the given user: shared wallets (no owner) plus the user's
    /// own personal wallets. Admins see every wallet. Ordered by name.
    /// </summary>
    Task<IReadOnlyList<WalletEntity>> ListAccessibleAsync(
        Guid? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    /// <summary>True when the wallet has at least one operation (even a soft-deleted one).</summary>
    Task<bool> HasOperationsAsync(Guid walletId, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new wallet and persists.</summary>
    Task AddAsync(WalletEntity wallet, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing wallet.</summary>
    Task UpdateAsync(WalletEntity wallet, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the wallet (interceptor rewrites the delete into <c>IsDeleted = true</c>).</summary>
    Task SoftDeleteAsync(WalletEntity wallet, CancellationToken cancellationToken = default);
}
