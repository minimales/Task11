namespace task11.ApplicationCore.Auth;

public interface ICurrentUser
{

    Guid? UserId { get; }

    string? Role { get; }

    bool IsAdmin { get; }

    bool IsAuthenticated { get; }
}
