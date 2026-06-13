using task11.Data.Entities.Enums;

namespace task11.ApplicationCore.Models;

public class OperationModel
{

    public Guid Id { get; set; }

    public Guid WalletId { get; set; }

    public Guid TypeId { get; set; }

    public string TypeName { get; set; } = string.Empty;

    public OperationKind Kind { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
