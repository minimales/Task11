using task11.ApplicationCore.Models;
using task11.ApplicationCore.Entities;

namespace task11.ApplicationCore.Services.Abstractions;

public interface IWalletService
{

    Task<IReadOnlyList<WalletModel>> GetAccessibleAsync(CancellationToken cancellationToken = default);

    Task<WalletModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WalletModel> CreateAsync(CreateWalletModel request, CancellationToken cancellationToken = default);

    Task<WalletModel> UpdateAsync(Guid id, UpdateWalletModel request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WalletEntity> EnsureCanAccessAsync(Guid walletId, CancellationToken cancellationToken = default);
}
