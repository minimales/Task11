namespace task11.ApplicationCore.Models;

/// <summary>Payload to update a user (admin-only). Password is optional; omit to keep the current one.</summary>
public sealed class UpdateUserModel
{
    /// <summary>Login name. 3..50, <c>^[a-zA-Z0-9_.-]+$</c>, unique among non-deleted users.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>New plaintext password (minimum 6 characters). When null/empty the current hash is kept.</summary>
    public string? Password { get; set; }

    /// <summary>"Admin" or "User".</summary>
    public string Role { get; set; } = "User";
}
