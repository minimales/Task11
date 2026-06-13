namespace task11.ApplicationCore.Models;

/// <summary>Successful login result carrying the signed JWT and its UTC expiry.</summary>
public sealed class AuthTokenModel
{
    /// <summary>The signed HS256 bearer token (redacted from logs by the sanitizer).</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>The token's absolute expiry instant, in UTC.</summary>
    public DateTime ExpiresAtUtc { get; set; }
}
