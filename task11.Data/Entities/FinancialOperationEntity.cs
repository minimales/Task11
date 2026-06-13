namespace task11.Data.Entities;

public class FinancialOperationEntity : BaseEntity
{
    public Guid OperationTypeId { get; set; }

    public OperationTypeEntity OperationType { get; set; } = null!;

    public Guid WalletId { get; set; }

    public WalletEntity Wallet { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public string? Note { get; set; }
}
