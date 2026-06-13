using task11.Data.Entities.Enums;

namespace task11.ApplicationCore.Models;

public class ReportModel
{

    public decimal TotalIncome { get; set; }

    public decimal TotalExpense { get; set; }

    public decimal NetResult { get; set; }

    public string Currency { get; set; } = string.Empty;

    public IReadOnlyList<ReportOperationLineModel> Operations { get; set; } = Array.Empty<ReportOperationLineModel>();
}

public class ReportOperationLineModel
{

    public Guid Id { get; set; }

    public Guid OperationTypeId { get; set; }

    public string OperationTypeName { get; set; } = string.Empty;

    public OperationKind Kind { get; set; }

    public decimal Amount { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public string? Note { get; set; }
}
