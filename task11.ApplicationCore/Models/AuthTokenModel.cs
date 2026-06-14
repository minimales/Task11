namespace task11.ApplicationCore.Models;

public class AuthTokenModel
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}
