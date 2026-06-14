using task11.ApplicationCore.Entities.Enums;

namespace task11.ApplicationCore.Models;

public class OperationTypeModel
{
    public Guid Id { get; set; }

    public Guid WalletId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public OperationKind Kind { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
