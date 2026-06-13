using task11.Data.Entities.Enums;

namespace task11.ApplicationCore.Models;

/// <summary>
/// Read model for a financial operation. The <see cref="Amount"/> is always in the
/// wallet base currency (<see cref="Currency"/>); any original transaction detail is in <see cref="Note"/>.
/// </summary>
public sealed class OperationModel
{
    /// <summary>The operation id.</summary>
    public Guid Id { get; set; }

    /// <summary>The owning wallet id.</summary>
    public Guid WalletId { get; set; }

    /// <summary>The operation type id.</summary>
    public Guid TypeId { get; set; }

    /// <summary>The operation type name (denormalized for convenience).</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>The operation kind (income/expense) inherited from the type.</summary>
    public OperationKind Kind { get; set; }

    /// <summary>The stored amount, always in the wallet base currency.</summary>
    public decimal Amount { get; set; }

    /// <summary>The wallet base currency the amount is expressed in.</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>The operation date (UTC).</summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Optional note, including any appended conversion-audit string.</summary>
    public string? Note { get; set; }

    /// <summary>Creation timestamp (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Last update timestamp (UTC), if any.</summary>
    public DateTime? UpdatedAtUtc { get; set; }
}
