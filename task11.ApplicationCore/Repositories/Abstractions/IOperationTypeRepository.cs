using task11.ApplicationCore.Entities;

namespace task11.ApplicationCore.Repositories.Abstractions;

public interface IOperationTypeRepository
{

    Task<OperationTypeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationTypeEntity>> ListByWalletAsync(Guid walletId, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        Guid walletId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(OperationTypeEntity type, CancellationToken cancellationToken = default);

    Task UpdateAsync(OperationTypeEntity type, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(OperationTypeEntity type, CancellationToken cancellationToken = default);
}
