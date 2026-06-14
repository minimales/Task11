namespace task11.ApplicationCore.Models;

public class CreateUserModel
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "User";
}
