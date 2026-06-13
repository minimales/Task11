using Microsoft.EntityFrameworkCore;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.Data.Entities;

namespace task11.ApplicationCore.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/>. Opens a fresh
/// <c>AppDbContext</c> per operation via the injected <see cref="DbContextFactory"/>.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly DbContextFactory _factory;

    public UserRepository(DbContextFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <inheritdoc />
    public async Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        return await ctx.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        return await ctx.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        return await ctx.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> UsernameExistsAsync(string username, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        return await ctx.Users
            .AsNoTracking()
            .AnyAsync(u => u.Username == username && (excludeId == null || u.Id != excludeId), cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        await ctx.Users.AddAsync(user, cancellationToken);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        ctx.Users.Update(user);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SoftDeleteAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();
        // Marked Removed; the SoftDeleteInterceptor rewrites this to a soft delete.
        ctx.Users.Remove(user);
        await ctx.SaveChangesAsync(cancellationToken);
    }
}
