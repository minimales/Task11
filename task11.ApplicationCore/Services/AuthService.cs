using Microsoft.Extensions.Logging;
using task11.ApplicationCore.Auth;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.ApplicationCore.Services.Abstractions;

namespace task11.ApplicationCore.Services;

/// <summary>
/// Verifies credentials against the stored PBKDF2 hash and issues a signed JWT on success.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly ILogger<AuthService> _logger;

    // A throwaway hash verified when the username is unknown, so the login path spends the same
    // PBKDF2 cost whether or not the user exists (mitigates timing-based username enumeration).
    private readonly string _dummyHash;

    public AuthService(
        IUserRepository users,
        PasswordHasher passwordHasher,
        JwtTokenGenerator tokenGenerator,
        ILogger<AuthService> logger)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _logger = logger;
        _dummyHash = passwordHasher.Hash("__timing_guard_not_a_real_password__");
    }

    /// <inheritdoc />
    public async Task<AuthTokenModel?> LoginAsync(LoginModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _users.GetByUsernameAsync(request.Username, cancellationToken);

        // Always run PBKDF2 (against a dummy hash when the user is unknown) so the response time
        // does not reveal whether the username exists. The && keeps the result correct: an unknown
        // user can never authenticate even if the dummy verify happened to pass.
        string storedHash = user?.PasswordHash ?? _dummyHash;
        bool passwordMatches = _passwordHasher.Verify(request.Password, storedHash);
        bool credentialsValid = user is not null && passwordMatches;

        if (!credentialsValid)
        {
            // Never log the submitted password; log only the (non-secret) username.
            _logger.LogWarning("Failed login attempt for username '{Username}'.", request.Username);
            return null;
        }

        var (token, expiresAtUtc) = _tokenGenerator.Generate(user!);

        _logger.LogInformation("User '{Username}' authenticated successfully.", user!.Username);

        return new AuthTokenModel
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc
        };
    }
}
