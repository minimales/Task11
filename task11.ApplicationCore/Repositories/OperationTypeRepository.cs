using Microsoft.EntityFrameworkCore;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.Data.Entities;

namespace task11.ApplicationCore.Repositories;

public class OperationTypeRepository : IOperationTypeRepository
{
    private readonly DbContextFactory _factory;

    public OperationTypeRepository(DbContextFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task<OperationTypeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        return await ctx.OperationTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

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

    public async Task AddAsync(OperationTypeEntity type, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        await ctx.OperationTypes.AddAsync(type, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(OperationTypeEntity type, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        ctx.OperationTypes.Update(type);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(OperationTypeEntity type, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        ctx.OperationTypes.Remove(type);
        await ctx.SaveChangesAsync(cancellationToken);
    }
}
