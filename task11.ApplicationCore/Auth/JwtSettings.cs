namespace task11.ApplicationCore.Auth;

/// <summary>
/// JWT configuration bound from the <c>Jwt</c> config section. The <see cref="Secret"/>
/// is supplied via the <c>JWT__Secret</c> environment variable and is never committed.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Token issuer.</summary>
    public string Issuer { get; set; } = "PersonalFinance";

    /// <summary>Token audience.</summary>
    public string Audience { get; set; } = "PersonalFinance";

    /// <summary>HMAC-SHA256 signing key. Must be at least 32 characters.</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Token lifetime in minutes.</summary>
    public int ExpiryMinutes { get; set; } = 60;
}
