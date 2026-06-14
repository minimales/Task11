using task11.ApplicationCore.Models;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.ApplicationCore.Services.Abstractions;
using task11.ApplicationCore.Entities;

namespace task11.ApplicationCore.Services;

public class OperationTypeService : IOperationTypeService
{
    private readonly IOperationTypeRepository _repository;
    private readonly IWalletService _walletService;

    public OperationTypeService(IOperationTypeRepository repository, IWalletService walletService)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(walletService);

        _repository = repository;
        _walletService = walletService;
    }

    public async Task<IReadOnlyList<OperationTypeModel>> GetByWalletAsync(
        Guid walletId,
        CancellationToken cancellationToken = default)
    {
        await _walletService.EnsureCanAccessAsync(walletId, cancellationToken);

        var types = await _repository.ListByWalletAsync(walletId, cancellationToken);
        return types.Select(Map).ToList();
    }

    public async Task<OperationTypeModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var type = await GetOwnedTypeAsync(id, cancellationToken);
        return Map(type);
    }

    public async Task<OperationTypeModel> CreateAsync(
        Guid walletId,
        CreateOperationTypeModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _walletService.EnsureCanAccessAsync(walletId, cancellationToken);

        if (await _repository.NameExistsAsync(walletId, request.Name, excludeId: null, cancellationToken))
        {
            throw new ConflictException(
                $"An operation type named '{request.Name}' already exists in this wallet.");
        }

        var type = new OperationTypeEntity
        {
            WalletId = walletId,
            Name = request.Name,
            Description = request.Description,
            Kind = request.Kind
        };

        await _repository.AddAsync(type, cancellationToken);

        return Map(type);
    }

    public async Task<OperationTypeModel> UpdateAsync(
        Guid id,
        UpdateOperationTypeModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var type = await GetOwnedTypeAsync(id, cancellationToken);

        if (await _repository.NameExistsAsync(type.WalletId, request.Name, excludeId: type.Id, cancellationToken))
        {
            throw new ConflictException(
                $"An operation type named '{request.Name}' already exists in this wallet.");
        }

        type.Name = request.Name;
        type.Description = request.Description;
        type.Kind = request.Kind;

        await _repository.UpdateAsync(type, cancellationToken);

        return Map(type);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var type = await GetOwnedTypeAsync(id, cancellationToken);

        await _repository.SoftDeleteAsync(type, cancellationToken);
    }

    private async Task<OperationTypeEntity> GetOwnedTypeAsync(Guid id, CancellationToken cancellationToken)
    {
        var type = await _repository.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(nameof(OperationTypeEntity), id);

        await _walletService.EnsureCanAccessAsync(type.WalletId, cancellationToken);

        return type;
    }

    private static OperationTypeModel Map(OperationTypeEntity type) => new()
    {
        Id = type.Id,
        WalletId = type.WalletId,
        Name = type.Name,
        Description = type.Description,
        Kind = type.Kind,
        CreatedAtUtc = type.CreatedAtUtc,
        UpdatedAtUtc = type.UpdatedAtUtc
    };
}
