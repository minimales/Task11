using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Services.Abstractions;

/// <summary>Authentication operations: verifying credentials and issuing JWTs.</summary>
public interface IAuthService
{
    /// <summary>
    /// Verifies the supplied credentials and, on success, issues a signed JWT.
    /// Returns <c>null</c> when the username is unknown or the password is wrong
    /// (the controller maps that to HTTP 401), so no exception is thrown for the
    /// expected bad-credentials case.
    /// </summary>
    Task<AuthTokenModel?> LoginAsync(LoginModel request, CancellationToken cancellationToken = default);
}
