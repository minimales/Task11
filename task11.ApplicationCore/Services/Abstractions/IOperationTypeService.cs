using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Services.Abstractions;

/// <summary>
/// Use-cases for wallet-scoped operation types. Every method first resolves the target
/// wallet and enforces ownership isolation via <see cref="IWalletService.EnsureCanAccessAsync"/>.
/// </summary>
public interface IOperationTypeService
{
    /// <summary>Lists the non-deleted operation types of an accessible wallet.</summary>
    /// <exception cref="NotFoundException">No such wallet.</exception>
    /// <exception cref="ForbiddenException">Wallet not accessible to the caller.</exception>
    Task<IReadOnlyList<OperationTypeModel>> GetByWalletAsync(
        Guid walletId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a single operation type by id, after verifying access to its wallet.</summary>
    /// <exception cref="NotFoundException">No such operation type.</exception>
    /// <exception cref="ForbiddenException">Wallet not accessible to the caller.</exception>
    Task<OperationTypeModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an operation type in an accessible wallet. The name must be unique among the
    /// wallet's non-deleted types.
    /// </summary>
    /// <exception cref="NotFoundException">No such wallet.</exception>
    /// <exception cref="ForbiddenException">Wallet not accessible to the caller.</exception>
    /// <exception cref="ConflictException">Duplicate name in the wallet.</exception>
    Task<OperationTypeModel> CreateAsync(
        Guid walletId,
        CreateOperationTypeModel request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an operation type after verifying access to its wallet. The new name must remain
    /// unique among the wallet's non-deleted types.
    /// </summary>
    /// <exception cref="NotFoundException">No such operation type.</exception>
    /// <exception cref="ForbiddenException">Wallet not accessible to the caller.</exception>
    /// <exception cref="ConflictException">Duplicate name in the wallet.</exception>
    Task<OperationTypeModel> UpdateAsync(
        Guid id,
        UpdateOperationTypeModel request,
        CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes an operation type after verifying access to its wallet.</summary>
    /// <exception cref="NotFoundException">No such operation type.</exception>
    /// <exception cref="ForbiddenException">Wallet not accessible to the caller.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
