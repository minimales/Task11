using task11.Data.Entities.Enums;

namespace task11.Data.Entities;

public class OperationTypeEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public OperationKind Kind { get; set; }

    public Guid WalletId { get; set; }

    public WalletEntity Wallet { get; set; } = null!;

    public ICollection<FinancialOperationEntity> Operations { get; set; } = new List<FinancialOperationEntity>();
}
