using task11.ApplicationCore.Auth;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.ApplicationCore.Services.Abstractions;
using task11.ApplicationCore.Entities;

namespace task11.ApplicationCore.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly PasswordHasher _passwordHasher;

    public UserService(IUserRepository users, PasswordHasher passwordHasher)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(passwordHasher);

        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<IReadOnlyList<UserModel>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<UserEntity> users = await _users.GetAllAsync(cancellationToken);
        return users.Select(Map).ToList();
    }

    public async Task<UserModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        UserEntity user = await _users.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(nameof(UserEntity), id);

        return Map(user);
    }

    public async Task<UserModel> CreateAsync(CreateUserModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await _users.UsernameExistsAsync(request.Username, excludeId: null, cancellationToken))
        {
            throw new ConflictException($"Username '{request.Username}' is already taken.");
        }

        UserEntity user = new UserEntity
        {
            Username = request.Username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role
        };

        await _users.AddAsync(user, cancellationToken);

        return Map(user);
    }

    public async Task<UserModel> UpdateAsync(Guid id, UpdateUserModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        UserEntity user = await _users.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(nameof(UserEntity), id);

        if (await _users.UsernameExistsAsync(request.Username, excludeId: id, cancellationToken))
        {
            throw new ConflictException($"Username '{request.Username}' is already taken.");
        }

        user.Username = request.Username;
        user.Role = request.Role;

        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = _passwordHasher.Hash(request.Password);
        }

        await _users.UpdateAsync(user, cancellationToken);

        return Map(user);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        UserEntity user = await _users.GetByIdAsync(id, cancellationToken)
                   ?? throw new NotFoundException(nameof(UserEntity), id);

        await _users.SoftDeleteAsync(user, cancellationToken);
    }

    private static UserModel Map(UserEntity user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Role = user.Role,
        CreatedAtUtc = user.CreatedAtUtc,
        UpdatedAtUtc = user.UpdatedAtUtc
    };
}
