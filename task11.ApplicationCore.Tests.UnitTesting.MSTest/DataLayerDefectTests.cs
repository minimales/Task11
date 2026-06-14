using task11.ApplicationCore;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.ApplicationCore.Services;
using task11.ApplicationCore.Entities;
using task11.ApplicationCore.Entities.Enums;
using task11.Infrastructure.Persistence;
using task11.Infrastructure.Repositories;

[TestClass]
public class DataLayerDefectTests
{
    private static readonly Guid _walletId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid _typeId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static WalletEntity SharedWallet() => new()
    {
        Id = _walletId,
        Name = "Shared",
        BaseCurrency = "UAH",
        OwnerUserId = null
    };

    [TestMethod]
    public async Task Test_OperationTypeService_DeleteAsync_TypeHasLiveOperation_ThrowsConflictException()
    {
        WalletEntity wallet = SharedWallet();

        OperationTypeEntity type = new()
        {
            Id = _typeId,
            WalletId = _walletId,
            Name = "Salary",
            Kind = OperationKind.Income
        };

        StatefulFakeOperationTypeRepository repo = new(type, hasOperations: true);
        FakeWalletService wallets = new(wallet);

        OperationTypeService service = new(repo, wallets);

        await Assert.ThrowsExceptionAsync<ConflictException>(() => service.DeleteAsync(_typeId));

        Assert.AreEqual(0, repo.SoftDeleteCallCount);
    }

    [TestMethod]
    public async Task Test_OperationTypeService_DeleteAsync_TypeHasNoLiveOperations_SoftDeletes()
    {
        WalletEntity wallet = SharedWallet();

        OperationTypeEntity type = new()
        {
            Id = _typeId,
            WalletId = _walletId,
            Name = "Salary",
            Kind = OperationKind.Income
        };

        StatefulFakeOperationTypeRepository repo = new(type, hasOperations: false);
        FakeWalletService wallets = new(wallet);

        OperationTypeService service = new(repo, wallets);

        await service.DeleteAsync(_typeId);

        Assert.AreEqual(1, repo.SoftDeleteCallCount);
    }

    [TestMethod]
    public async Task Test_WalletRepository_HasOperationsAsync_OnlySoftDeletedOperation_ReturnsFalse()
    {
        InMemoryDbContextFactory factory = new(
            nameof(Test_WalletRepository_HasOperationsAsync_OnlySoftDeletedOperation_ReturnsFalse));

        OperationTypeEntity type = new() { Id = _typeId, Name = "Salary", Kind = OperationKind.Income, WalletId = _walletId };

        using (AppDbContext db = factory.CreateDbContext())
        {
            db.OperationTypes.Add(type);

            db.FinancialOperations.Add(new FinancialOperationEntity
            {
                OperationTypeId = _typeId,
                WalletId = _walletId,
                Amount = 100m,
                OccurredAtUtc = new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc),
                IsDeleted = true,
                DeletedAtUtc = new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc)
            });

            db.SaveChanges();
        }

        WalletRepository repository = new(factory);

        bool hasOperations = await repository.HasOperationsAsync(_walletId);

        Assert.IsFalse(hasOperations);
    }

    [TestMethod]
    public async Task Test_WalletRepository_HasOperationsAsync_LiveOperation_ReturnsTrue()
    {
        InMemoryDbContextFactory factory = new(
            nameof(Test_WalletRepository_HasOperationsAsync_LiveOperation_ReturnsTrue));

        OperationTypeEntity type = new() { Id = _typeId, Name = "Salary", Kind = OperationKind.Income, WalletId = _walletId };

        using (AppDbContext db = factory.CreateDbContext())
        {
            db.OperationTypes.Add(type);

            db.FinancialOperations.Add(new FinancialOperationEntity
            {
                OperationTypeId = _typeId,
                WalletId = _walletId,
                Amount = 100m,
                OccurredAtUtc = new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc)
            });

            db.SaveChanges();
        }

        WalletRepository repository = new(factory);

        bool hasOperations = await repository.HasOperationsAsync(_walletId);

        Assert.IsTrue(hasOperations);
    }
}

internal class StatefulFakeOperationTypeRepository : IOperationTypeRepository
{
    private readonly OperationTypeEntity _type;
    private readonly bool _hasOperations;

    public int SoftDeleteCallCount { get; private set; }

    public StatefulFakeOperationTypeRepository(OperationTypeEntity type, bool hasOperations)
    {
        _type = type;
        _hasOperations = hasOperations;
    }

    public Task<OperationTypeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<OperationTypeEntity?>(id == _type.Id ? _type : null);

    public Task<IReadOnlyList<OperationTypeEntity>> ListByWalletAsync(Guid walletId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<bool> NameExistsAsync(Guid walletId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<bool> HasOperationsAsync(Guid operationTypeId, CancellationToken cancellationToken = default)
        => Task.FromResult(_hasOperations);

    public Task AddAsync(OperationTypeEntity type, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task UpdateAsync(OperationTypeEntity type, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SoftDeleteAsync(OperationTypeEntity type, CancellationToken cancellationToken = default)
    {
        SoftDeleteCallCount++;
        return Task.CompletedTask;
    }
}
