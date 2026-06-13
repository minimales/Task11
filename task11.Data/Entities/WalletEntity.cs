namespace task11.Data.Entities;

public class WalletEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string BaseCurrency { get; set; } = "UAH";

    public Guid? OwnerUserId { get; set; }

    public UserEntity? Owner { get; set; }

    public ICollection<FinancialOperationEntity> Operations { get; set; } = new List<FinancialOperationEntity>();

    public ICollection<OperationTypeEntity> OperationTypes { get; set; } = new List<OperationTypeEntity>();
}
