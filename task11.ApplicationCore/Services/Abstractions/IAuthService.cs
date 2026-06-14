using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Services.Abstractions;

public interface IAuthService
{
    Task<AuthTokenModel?> LoginAsync(LoginModel request, CancellationToken cancellationToken = default);
}
