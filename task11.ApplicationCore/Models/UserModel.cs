namespace task11.ApplicationCore.Models;

/// <summary>Public projection of a user. Never carries the password hash.</summary>
public sealed class UserModel
{
    /// <summary>The user's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>The user's login name.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>"Admin" or "User".</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>When the user was created (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>When the user was last updated (UTC), or null if never updated.</summary>
    public DateTime? UpdatedAtUtc { get; set; }
}
