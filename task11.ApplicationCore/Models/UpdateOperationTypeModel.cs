using task11.Data.Entities.Enums;

namespace task11.ApplicationCore.Models;

/// <summary>
/// Request body for updating an existing operation type. The wallet scope is immutable;
/// only the descriptive fields and kind may change.
/// </summary>
public sealed class UpdateOperationTypeModel
{
    /// <summary>Required name, 1..100 chars. Unique per wallet among non-deleted types.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description, &lt;= 500 chars.</summary>
    public string? Description { get; set; }

    /// <summary>Income or Expense — drives report totals.</summary>
    public OperationKind Kind { get; set; }
}
