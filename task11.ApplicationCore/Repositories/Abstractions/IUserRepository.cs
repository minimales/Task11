using task11.Data.Entities;

namespace task11.ApplicationCore.Repositories.Abstractions;

/// <summary>
/// Data-access contract for <see cref="UserEntity"/>. Reads honour the global soft-delete query
/// filter, so all lookups only ever see non-deleted users. Each method opens its own context.
/// </summary>
public interface IUserRepository
{
    /// <summary>Returns a user by id, or null when not found / soft-deleted.</summary>
    Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all non-deleted users, ordered by username.</summary>
    Task<IReadOnlyList<UserEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the non-deleted user with the given username, or null when none exists.</summary>
    Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when a non-deleted user with the given username exists, optionally
    /// ignoring the user identified by <paramref name="excludeId"/> (used on update).
    /// </summary>
    Task<bool> UsernameExistsAsync(string username, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new user and persists.</summary>
    Task AddAsync(UserEntity user, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing user.</summary>
    Task UpdateAsync(UserEntity user, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the user (interceptor rewrites the delete into <c>IsDeleted = true</c>).</summary>
    Task SoftDeleteAsync(UserEntity user, CancellationToken cancellationToken = default);
}
