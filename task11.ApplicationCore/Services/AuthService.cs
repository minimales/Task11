using Microsoft.Extensions.Logging;
using task11.ApplicationCore.Auth;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.ApplicationCore.Services.Abstractions;

namespace task11.ApplicationCore.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly ILogger<AuthService> _logger;

    private readonly string _dummyHash;

    public AuthService(
        IUserRepository users,
        PasswordHasher passwordHasher,
        JwtTokenGenerator tokenGenerator,
        ILogger<AuthService> logger)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(tokenGenerator);
        ArgumentNullException.ThrowIfNull(logger);

        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _logger = logger;
        _dummyHash = passwordHasher.Hash("__timing_guard_not_a_real_password__");
    }

    public async Task<AuthTokenModel?> LoginAsync(LoginModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Entities.UserEntity? user = await _users.GetByUsernameAsync(request.Username, cancellationToken);

        string storedHash = user?.PasswordHash ?? _dummyHash;
        bool passwordMatches = _passwordHasher.Verify(request.Password, storedHash);
        bool credentialsValid = user is not null && passwordMatches;

        if (!credentialsValid)
        {
            _logger.LogWarning("Failed login attempt for username '{Username}'.", request.Username);
            return null;
        }

        (string token, DateTime expiresAtUtc) = _tokenGenerator.Generate(user!);

        _logger.LogInformation("User '{Username}' authenticated successfully.", user!.Username);

        return new AuthTokenModel
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc
        };
    }
}
