namespace task11.ApplicationCore.Auth;

/// <summary>
/// Ambient information about the authenticated caller, resolved from the request claims.
/// Registered Scoped. The implementation (reading <c>HttpContext</c>) lives in the Web layer.
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated user's id, or null when anonymous.</summary>
    Guid? UserId { get; }

    /// <summary>The authenticated user's role, or null when anonymous.</summary>
    string? Role { get; }

    /// <summary>True when the caller is in the "Admin" role.</summary>
    bool IsAdmin { get; }

    /// <summary>True when a user id is present on the request.</summary>
    bool IsAuthenticated { get; }
}
