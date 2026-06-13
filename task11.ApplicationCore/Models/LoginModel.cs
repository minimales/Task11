namespace task11.ApplicationCore.Models;

/// <summary>Credentials submitted to <c>POST /api/auth/login</c>.</summary>
public sealed class LoginModel
{
    /// <summary>The user's login name.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>The user's plaintext password (redacted from logs by the sanitizer).</summary>
    public string Password { get; set; } = string.Empty;
}
