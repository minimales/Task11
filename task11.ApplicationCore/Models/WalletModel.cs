namespace task11.ApplicationCore.Models;

public class WalletModel
{

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string BaseCurrency { get; set; } = string.Empty;

    public Guid? OwnerUserId { get; set; }

    public bool IsShared => OwnerUserId is null;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
