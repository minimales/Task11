using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Services.Abstractions;

public interface IUserService
{

    Task<IReadOnlyList<UserModel>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<UserModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserModel> CreateAsync(CreateUserModel request, CancellationToken cancellationToken = default);

    Task<UserModel> UpdateAsync(Guid id, UpdateUserModel request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
