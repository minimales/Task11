using task11.ApplicationCore.Entities;

namespace task11.ApplicationCore.Repositories.Abstractions;

public interface IOperationRepository
{

    Task<FinancialOperationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FinancialOperationEntity?> GetWithTypeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialOperationEntity>> ListByWalletAsync(Guid walletId, CancellationToken cancellationToken = default);

    Task<OperationTypeEntity?> GetTypeForWalletAsync(Guid typeId, Guid walletId, CancellationToken cancellationToken = default);

    Task AddAsync(FinancialOperationEntity operation, CancellationToken cancellationToken = default);

    Task UpdateAsync(FinancialOperationEntity operation, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(FinancialOperationEntity operation, CancellationToken cancellationToken = default);
}
