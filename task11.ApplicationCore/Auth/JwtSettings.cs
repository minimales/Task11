namespace task11.ApplicationCore.Auth;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "PersonalFinance";

    public string Audience { get; set; } = "PersonalFinance";

    public string Secret { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}
