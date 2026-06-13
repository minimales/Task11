namespace task11.Data.Entities;

/// <summary>
/// A single income or expense entry. The <see cref="Amount"/> is always stored in the
/// owning wallet's base currency; the original transaction is appended to <see cref="Note"/>
/// after currency conversion.
/// </summary>
public class FinancialOperationEntity : BaseEntity
{
    /// <summary>FK to the operation type (carries the Kind).</summary>
    public Guid OperationTypeId { get; set; }

    /// <summary>Operation type navigation (carries Kind).</summary>
    public OperationTypeEntity OperationType { get; set; } = null!;

    /// <summary>Denormalized wallet FK for fast scoped queries and isolation.</summary>
    public Guid WalletId { get; set; }

    /// <summary>Owning wallet navigation.</summary>
    public WalletEntity Wallet { get; set; } = null!;

    /// <summary>Amount in the wallet base currency. numeric(18,2).</summary>
    public decimal Amount { get; set; }

    /// <summary>The operation "date". Always UTC. Used for FX lookup and reporting.</summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Optional note (&lt;= 500 user chars); a converted-original audit string may be appended.</summary>
    public string? Note { get; set; }
}
