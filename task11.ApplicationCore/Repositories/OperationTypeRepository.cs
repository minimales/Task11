using Microsoft.EntityFrameworkCore;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.Data.Entities;

namespace task11.ApplicationCore.Repositories;

/// <summary>
/// EF Core repository for <see cref="OperationTypeEntity"/>. Each method opens a fresh
/// context via <see cref="DbContextFactory"/>; reads honour the soft-delete query filter.
/// </summary>
public sealed class OperationTypeRepository : IOperationTypeRepository
{
    private readonly DbContextFactory _factory;

    public OperationTypeRepository(DbContextFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <inheritdoc />
    public async Task<OperationTypeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        return await ctx.OperationTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OperationTypeEntity>> ListByWalletAsync(
        Guid walletId,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        return await ctx.OperationTypes
            .AsNoTracking()
            .Where(t => t.WalletId == walletId)
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> NameExistsAsync(
        Guid walletId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        return await ctx.OperationTypes
            .AsNoTracking()
            .AnyAsync(
                t => t.WalletId == walletId
                     && t.Name == name
                     && (excludeId == null || t.Id != excludeId),
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(OperationTypeEntity type, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        await ctx.OperationTypes.AddAsync(type, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(OperationTypeEntity type, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        ctx.OperationTypes.Update(type);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SoftDeleteAsync(OperationTypeEntity type, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        ctx.OperationTypes.Remove(type);
        await ctx.SaveChangesAsync(cancellationToken);
    }
}
