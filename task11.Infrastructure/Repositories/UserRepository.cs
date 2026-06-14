using Microsoft.EntityFrameworkCore;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.Infrastructure.Persistence;
using task11.ApplicationCore.Entities;

namespace task11.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DbContextFactory _factory;

    public UserRepository(DbContextFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using AppDbContext ctx = _factory.CreateDbContext();
        return await ctx.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<UserEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext ctx = _factory.CreateDbContext();
        return await ctx.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        await using AppDbContext ctx = _factory.CreateDbContext();
        return await ctx.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<bool> UsernameExistsAsync(string username, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        await using AppDbContext ctx = _factory.CreateDbContext();
        return await ctx.Users
            .AsNoTracking()
            .AnyAsync(u => u.Username == username && (excludeId == null || u.Id != excludeId), cancellationToken);
    }

    public async Task AddAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using AppDbContext ctx = _factory.CreateDbContext();
        await ctx.Users.AddAsync(user, cancellationToken);
        await UniqueViolationTranslator.SaveChangesTranslatingUniqueViolationAsync(
            ctx,
            $"A user named '{user.Username}' already exists.",
            cancellationToken);
    }

    public async Task UpdateAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using AppDbContext ctx = _factory.CreateDbContext();
        ctx.Users.Update(user);
        await UniqueViolationTranslator.SaveChangesTranslatingUniqueViolationAsync(
            ctx,
            $"A user named '{user.Username}' already exists.",
            cancellationToken);
    }

    public async Task SoftDeleteAsync(UserEntity user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using AppDbContext ctx = _factory.CreateDbContext();

        ctx.Users.Remove(user);
        await ctx.SaveChangesAsync(cancellationToken);
    }
}
