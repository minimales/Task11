using Microsoft.EntityFrameworkCore;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.Data.Entities;

namespace task11.ApplicationCore.Repositories;

/// <summary>
/// EF Core repository for <see cref="FinancialOperationEntity"/>. Each method opens a fresh
/// context via <see cref="DbContextFactory"/>; reads honour the soft-delete query filter.
/// </summary>
public sealed class OperationRepository : IOperationRepository
{
    private readonly DbContextFactory _factory;

    public OperationRepository(DbContextFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <inheritdoc />
    public async Task<FinancialOperationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        return await ctx.FinancialOperations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FinancialOperationEntity?> GetWithTypeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        return await ctx.FinancialOperations
            .AsNoTracking()
            .Include(o => o.OperationType)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FinancialOperationEntity>> ListByWalletAsync(
        Guid walletId,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        return await ctx.FinancialOperations
            .AsNoTracking()
            .Include(o => o.OperationType)
            .Where(o => o.WalletId == walletId)
            .OrderByDescending(o => o.OccurredAtUtc)
            .ThenByDescending(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OperationTypeEntity?> GetTypeForWalletAsync(
        Guid typeId,
        Guid walletId,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        return await ctx.OperationTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == typeId && t.WalletId == walletId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(FinancialOperationEntity operation, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        await ctx.FinancialOperations.AddAsync(operation, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(FinancialOperationEntity operation, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        ctx.FinancialOperations.Update(operation);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SoftDeleteAsync(FinancialOperationEntity operation, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        ctx.FinancialOperations.Remove(operation);
        await ctx.SaveChangesAsync(cancellationToken);
    }
}
