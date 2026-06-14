using System.Security.Claims;
using task11.ApplicationCore.Auth;

namespace task11.Web.Infrastructure.Auth;

public class CurrentUser : ICurrentUser
{
    private const string _adminRole = "Admin";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public Guid? UserId
    {
        get
        {
            string? sub = Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(sub, out Guid id) ? id : null;
        }
    }

    public string? Role =>
        Principal?.FindFirstValue(ClaimTypes.Role) ?? Principal?.FindFirstValue("role");

    public bool IsAdmin => string.Equals(Role, _adminRole, StringComparison.OrdinalIgnoreCase);

    public bool IsAuthenticated => UserId is not null;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        _httpContextAccessor = httpContextAccessor;
    }
}
