namespace task11.Data.Entities;

/// <summary>
/// Application user. Authenticated via JWT; the <see cref="PasswordHash"/> is never
/// logged or serialized to clients.
/// </summary>
public class UserEntity : BaseEntity
{
    /// <summary>Unique login name (filtered unique index). 3..50, ^[a-zA-Z0-9_.-]+$.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>PBKDF2 hash in the form {iterations}.{salt}.{hash}. Never logged/serialized.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>"Admin" | "User". Defaults to "User".</summary>
    public string Role { get; set; } = "User";

    /// <summary>Wallets owned by this user.</summary>
    public ICollection<WalletEntity> OwnedWallets { get; set; } = new List<WalletEntity>();
}
