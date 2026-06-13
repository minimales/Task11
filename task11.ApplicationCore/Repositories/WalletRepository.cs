using Microsoft.EntityFrameworkCore;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.Data.Entities;

namespace task11.ApplicationCore.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IWalletRepository"/>. Reads honour the global
/// soft-delete query filter; each method opens a fresh context via <see cref="DbContextFactory"/>.
/// </summary>
public sealed class WalletRepository : IWalletRepository
{
    private readonly DbContextFactory _factory;

    public WalletRepository(DbContextFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <inheritdoc />
    public async Task<WalletEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        return await ctx.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WalletEntity>> ListAccessibleAsync(
        Guid? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();

        IQueryable<WalletEntity> query = ctx.Wallets.AsNoTracking();

        if (!isAdmin)
        {
            // Shared wallets (no owner) plus the caller's own personal wallets.
            query = query.Where(w => w.OwnerUserId == null || w.OwnerUserId == userId);
        }

        return await query
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasOperationsAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();

        // Ignore the soft-delete filter: a wallet that ever had operations (even soft-deleted ones,
        // whose Amount is stored in the old base currency) must keep its BaseCurrency immutable.
        return await ctx.FinancialOperations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AnyAsync(o => o.WalletId == walletId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(WalletEntity wallet, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        await ctx.Wallets.AddAsync(wallet, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(WalletEntity wallet, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        ctx.Wallets.Update(wallet);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SoftDeleteAsync(WalletEntity wallet, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        ctx.Wallets.Remove(wallet);
        await ctx.SaveChangesAsync(cancellationToken);
    }
}
