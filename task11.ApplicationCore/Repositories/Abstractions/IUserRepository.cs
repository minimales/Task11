using task11.Data.Entities;

namespace task11.ApplicationCore.Repositories.Abstractions;

public interface IUserRepository
{

    Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(string username, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task AddAsync(UserEntity user, CancellationToken cancellationToken = default);

    Task UpdateAsync(UserEntity user, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(UserEntity user, CancellationToken cancellationToken = default);
}
