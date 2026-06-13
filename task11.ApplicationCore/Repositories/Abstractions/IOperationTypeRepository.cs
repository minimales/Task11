using task11.Data.Entities;

namespace task11.ApplicationCore.Repositories.Abstractions;

/// <summary>
/// Data-access contract for <see cref="OperationTypeEntity"/>. Reads honour the soft-delete
/// query filter, so deleted types are invisible to every method here. Each method opens its own context.
/// </summary>
public interface IOperationTypeRepository
{
    /// <summary>Returns an operation type by id, or null when not found / soft-deleted.</summary>
    Task<OperationTypeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all non-deleted operation types for a wallet, ordered by name.</summary>
    Task<IReadOnlyList<OperationTypeEntity>> ListByWalletAsync(Guid walletId, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when another non-deleted operation type in the same wallet already uses
    /// <paramref name="name"/>. <paramref name="excludeId"/> lets an update ignore itself.
    /// </summary>
    Task<bool> NameExistsAsync(
        Guid walletId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Inserts a new operation type and persists.</summary>
    Task AddAsync(OperationTypeEntity type, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing operation type.</summary>
    Task UpdateAsync(OperationTypeEntity type, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the operation type (interceptor rewrites the delete into <c>IsDeleted = true</c>).</summary>
    Task SoftDeleteAsync(OperationTypeEntity type, CancellationToken cancellationToken = default);
}
