using System.Security.Claims;
using task11.ApplicationCore.Auth;

namespace task11.Web.Infrastructure.Auth;

/// <summary>
/// Reads the current user's identity from the <see cref="HttpContext"/> claims.
/// Implements the ApplicationCore <see cref="ICurrentUser"/> abstraction.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private const string _adminRole = "Admin";

    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public string? Role =>
        Principal?.FindFirstValue(ClaimTypes.Role) ?? Principal?.FindFirstValue("role");

    /// <inheritdoc />
    public bool IsAdmin => string.Equals(Role, _adminRole, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public bool IsAuthenticated => UserId is not null;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        _httpContextAccessor = httpContextAccessor;
    }
}
