namespace task11.ApplicationCore.Entities;

public class UserEntity : BaseEntity
{
    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "User";

    public ICollection<WalletEntity> OwnedWallets { get; set; } = new List<WalletEntity>();
}
