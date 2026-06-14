using Microsoft.EntityFrameworkCore;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.Infrastructure.Persistence;
using task11.ApplicationCore.Entities;

namespace task11.Infrastructure.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly DbContextFactory _factory;

    public WalletRepository(DbContextFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task<WalletEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using AppDbContext ctx = _factory.CreateDbContext();
        return await ctx.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<WalletEntity>> ListAccessibleAsync(
        Guid? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        await using AppDbContext ctx = _factory.CreateDbContext();

        IQueryable<WalletEntity> query = ctx.Wallets.AsNoTracking();

        if (!isAdmin)
        {

            query = query.Where(w => w.OwnerUserId == null || w.OwnerUserId == userId);
        }

        return await query
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOperationsAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        await using AppDbContext ctx = _factory.CreateDbContext();

        return await ctx.FinancialOperations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AnyAsync(o => o.WalletId == walletId && !o.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(WalletEntity wallet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(wallet);

        await using AppDbContext ctx = _factory.CreateDbContext();
        await ctx.Wallets.AddAsync(wallet, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(WalletEntity wallet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(wallet);

        await using AppDbContext ctx = _factory.CreateDbContext();
        ctx.Wallets.Update(wallet);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(WalletEntity wallet, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(wallet);

        await using AppDbContext ctx = _factory.CreateDbContext();
        ctx.Wallets.Remove(wallet);
        await ctx.SaveChangesAsync(cancellationToken);
    }
}
