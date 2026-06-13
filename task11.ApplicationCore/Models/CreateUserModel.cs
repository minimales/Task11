namespace task11.ApplicationCore.Models;

/// <summary>Payload to create a user (admin-only).</summary>
public sealed class CreateUserModel
{
    /// <summary>Login name. 3..50, <c>^[a-zA-Z0-9_.-]+$</c>, unique among non-deleted users.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Plaintext password, minimum 6 characters. Hashed with PBKDF2 before storage.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>"Admin" or "User". Defaults to "User" when omitted.</summary>
    public string Role { get; set; } = "User";
}
