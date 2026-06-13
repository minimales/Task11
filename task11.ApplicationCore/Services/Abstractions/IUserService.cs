using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Services.Abstractions;

/// <summary>
/// User administration (admin-only): CRUD with PBKDF2 password hashing and soft delete.
/// The password hash is never returned in any model.
/// </summary>
public interface IUserService
{
    /// <summary>Returns all non-deleted users, ordered by username.</summary>
    Task<IReadOnlyList<UserModel>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the user with the given id, or throws <see cref="NotFoundException"/> (404) when absent.</summary>
    Task<UserModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a user with a PBKDF2-hashed password. Throws <see cref="ConflictException"/> (409)
    /// when the username is already taken among non-deleted users.
    /// </summary>
    Task<UserModel> CreateAsync(CreateUserModel request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's username, role and (optionally) password. Throws <see cref="NotFoundException"/> (404)
    /// when the user does not exist and <see cref="ConflictException"/> (409) on a username collision.
    /// </summary>
    Task<UserModel> UpdateAsync(Guid id, UpdateUserModel request, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the user. Throws <see cref="NotFoundException"/> (404) when the user does not exist.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
