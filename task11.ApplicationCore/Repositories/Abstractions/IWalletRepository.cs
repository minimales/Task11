using task11.ApplicationCore.Entities;

namespace task11.ApplicationCore.Repositories.Abstractions;

public interface IWalletRepository
{
    Task<WalletEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WalletEntity>> ListAccessibleAsync(
        Guid? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<bool> HasOperationsAsync(Guid walletId, CancellationToken cancellationToken = default);

    Task AddAsync(WalletEntity wallet, CancellationToken cancellationToken = default);

    Task UpdateAsync(WalletEntity wallet, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(WalletEntity wallet, CancellationToken cancellationToken = default);
}
