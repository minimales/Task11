using task11.Data.Entities.Enums;

namespace task11.ApplicationCore.Models;

/// <summary>
/// Request body for creating a wallet-scoped operation type.
/// The owning wallet is taken from the route.
/// </summary>
public sealed class CreateOperationTypeModel
{
    /// <summary>Required name, 1..100 chars. Unique per wallet among non-deleted types.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description, &lt;= 500 chars.</summary>
    public string? Description { get; set; }

    /// <summary>Income or Expense — drives report totals.</summary>
    public OperationKind Kind { get; set; }
}
