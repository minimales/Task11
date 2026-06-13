using task11.Data.Entities;

namespace task11.ApplicationCore.Repositories.Abstractions;

/// <summary>
/// Data-access contract for <see cref="FinancialOperationEntity"/> plus the wallet/type lookups
/// the operation service needs to enforce ownership isolation and FK existence without reaching
/// into <c>AppDbContext</c> directly. All reads honour the soft-delete query filter; each method
/// opens its own context.
/// </summary>
public interface IOperationRepository
{
    /// <summary>Returns an operation by id, or null when not found / soft-deleted.</summary>
    Task<FinancialOperationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the operation with its type navigation loaded, or null when missing/soft-deleted.
    /// </summary>
    Task<FinancialOperationEntity?> GetWithTypeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all (non-deleted) operations for a wallet, type loaded, newest first.
    /// </summary>
    Task<IReadOnlyList<FinancialOperationEntity>> ListByWalletAsync(Guid walletId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the operation type by id when it belongs to the given wallet (and is not deleted), else null.
    /// </summary>
    Task<OperationTypeEntity?> GetTypeForWalletAsync(Guid typeId, Guid walletId, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new operation and persists.</summary>
    Task AddAsync(FinancialOperationEntity operation, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing operation.</summary>
    Task UpdateAsync(FinancialOperationEntity operation, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the operation (interceptor rewrites the delete into <c>IsDeleted = true</c>).</summary>
    Task SoftDeleteAsync(FinancialOperationEntity operation, CancellationToken cancellationToken = default);
}
