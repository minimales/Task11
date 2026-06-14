namespace task11.ApplicationCore.Models;

public class UpdateUserModel
{
    public string Username { get; set; } = string.Empty;

    public string? Password { get; set; }

    public string Role { get; set; } = "User";
}
