using task11.Data.Entities.Enums;

namespace task11.Data.Entities;

/// <summary>
/// A wallet-scoped category for operations (e.g. "Salary"). Carries the
/// <see cref="OperationKind"/> that every operation inherits for report totals.
/// </summary>
public class OperationTypeEntity : BaseEntity
{
    /// <summary>Required, 1..100. e.g. "Salary".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description, &lt;= 500 chars.</summary>
    public string? Description { get; set; }

    /// <summary>Income/Expense — drives report totals.</summary>
    public OperationKind Kind { get; set; }

    /// <summary>FK to the owning wallet; types are wallet-scoped.</summary>
    public Guid WalletId { get; set; }

    /// <summary>Owning wallet navigation.</summary>
    public WalletEntity Wallet { get; set; } = null!;

    /// <summary>Operations of this type.</summary>
    public ICollection<FinancialOperationEntity> Operations { get; set; } = new List<FinancialOperationEntity>();
}
