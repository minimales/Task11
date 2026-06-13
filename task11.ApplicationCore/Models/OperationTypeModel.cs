using task11.Data.Entities.Enums;

namespace task11.ApplicationCore.Models;

/// <summary>
/// API representation of an operation type.
/// </summary>
public sealed class OperationTypeModel
{
    /// <summary>The operation type identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>The owning wallet identifier.</summary>
    public Guid WalletId { get; set; }

    /// <summary>The type name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Income or Expense.</summary>
    public OperationKind Kind { get; set; }

    /// <summary>Creation timestamp (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Last update timestamp (UTC), when modified.</summary>
    public DateTime? UpdatedAtUtc { get; set; }
}
