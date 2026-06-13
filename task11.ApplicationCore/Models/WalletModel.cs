namespace task11.ApplicationCore.Models;

/// <summary>
/// Read model returned for wallet endpoints. Entities are never exposed directly.
/// </summary>
public sealed class WalletModel
{
    /// <summary>The wallet identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>ISO-4217 base currency.</summary>
    public string BaseCurrency { get; set; } = string.Empty;

    /// <summary>Owning user id; null for a shared wallet.</summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>True when the wallet is shared with every user (no owner).</summary>
    public bool IsShared => OwnerUserId is null;

    /// <summary>When the wallet was created (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>When the wallet was last updated (UTC), if ever.</summary>
    public DateTime? UpdatedAtUtc { get; set; }
}
