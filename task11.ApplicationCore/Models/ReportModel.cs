using task11.Data.Entities.Enums;

namespace task11.ApplicationCore.Models;

/// <summary>
/// The result of a daily or period report. Totals are computed server-side via
/// <c>SUM(Amount)</c> grouped by <see cref="OperationKind"/> over a UTC date range.
/// All monetary values are expressed in the wallet's base <see cref="Currency"/>.
/// </summary>
public sealed class ReportModel
{
    /// <summary>Sum of all income operations in the range (wallet base currency).</summary>
    public decimal TotalIncome { get; set; }

    /// <summary>Sum of all expense operations in the range (wallet base currency).</summary>
    public decimal TotalExpense { get; set; }

    /// <summary>Net result: <c>TotalIncome - TotalExpense</c> (wallet base currency).</summary>
    public decimal NetResult { get; set; }

    /// <summary>ISO-4217 base currency of the wallet (e.g. "UAH").</summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>The individual operations that fall within the report range.</summary>
    public IReadOnlyList<ReportOperationLineModel> Operations { get; set; } = Array.Empty<ReportOperationLineModel>();
}

/// <summary>
/// A single operation line within a report. This is the report module's own projection
/// of a financial operation, kept independent of the OperationModule's DTOs.
/// </summary>
public sealed class ReportOperationLineModel
{
    /// <summary>The operation id.</summary>
    public Guid Id { get; set; }

    /// <summary>The operation type id this operation belongs to.</summary>
    public Guid OperationTypeId { get; set; }

    /// <summary>The operation type name (e.g. "Salary").</summary>
    public string OperationTypeName { get; set; } = string.Empty;

    /// <summary>The kind (Income/Expense) inherited from the operation type.</summary>
    public OperationKind Kind { get; set; }

    /// <summary>The amount in the wallet base currency.</summary>
    public decimal Amount { get; set; }

    /// <summary>When the operation occurred (UTC).</summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Optional note (may include the converted-original audit string).</summary>
    public string? Note { get; set; }
}
