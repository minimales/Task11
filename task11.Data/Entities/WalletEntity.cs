namespace task11.Data.Entities;

/// <summary>
/// A wallet holds operations and operation types in a single base currency.
/// A null <see cref="OwnerUserId"/> means the wallet is shared with every user.
/// </summary>
public class WalletEntity : BaseEntity
{
    /// <summary>Display name. Required, 1..100.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>ISO-4217 base currency ^[A-Z]{3}$, default "UAH", stored as CHAR(3). Immutable once operations exist.</summary>
    public string BaseCurrency { get; set; } = "UAH";

    /// <summary>NULL = shared wallet (all users); non-null = personal (owner only).</summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>Owner navigation (null for shared wallets).</summary>
    public UserEntity? Owner { get; set; }

    /// <summary>Operations recorded against this wallet.</summary>
    public ICollection<FinancialOperationEntity> Operations { get; set; } = new List<FinancialOperationEntity>();

    /// <summary>Operation types scoped to this wallet.</summary>
    public ICollection<OperationTypeEntity> OperationTypes { get; set; } = new List<OperationTypeEntity>();
}
