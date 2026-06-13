using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Services.Abstractions;

public interface IOperationTypeService
{

    Task<IReadOnlyList<OperationTypeModel>> GetByWalletAsync(
        Guid walletId,
        CancellationToken cancellationToken = default);

    Task<OperationTypeModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<OperationTypeModel> CreateAsync(
        Guid walletId,
        CreateOperationTypeModel request,
        CancellationToken cancellationToken = default);

    Task<OperationTypeModel> UpdateAsync(
        Guid id,
        UpdateOperationTypeModel request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
