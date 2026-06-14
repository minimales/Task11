using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Services.Abstractions;

public interface IOperationService
{
    Task<IReadOnlyList<OperationModel>> GetByWalletAsync(Guid walletId, CancellationToken cancellationToken = default);

    Task<OperationModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OperationModel> CreateAsync(CreateOperationModel request, CancellationToken cancellationToken = default);

    Task<OperationModel> UpdateAsync(Guid id, UpdateOperationModel request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
