using task11.Data.Entities;
using task11.Data.Entities.Enums;

namespace task11.ApplicationCore.Repositories.Abstractions;

/// <summary>
/// Read-only data access for report aggregation. Totals are computed server-side
/// with <c>SUM(Amount)</c> grouped by <see cref="OperationKind"/> over a half-open
/// UTC range <c>[fromUtc, toUtc)</c>; soft-deleted rows are excluded by the global query filter.
/// </summary>
public interface IReportRepository
{
    /// <summary>
    /// Returns income/expense totals for a wallet over the half-open UTC range
    /// <c>[fromUtc, toUtc)</c>, grouped by <see cref="OperationKind"/> server-side.
    /// </summary>
    Task<ReportTotals> GetTotalsAsync(
        Guid walletId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the operations (with their type) for a wallet over the half-open UTC range
    /// <c>[fromUtc, toUtc)</c>, ordered by <see cref="FinancialOperationEntity.OccurredAtUtc"/>.
    /// Soft-deleted rows are excluded.
    /// </summary>
    Task<IReadOnlyList<FinancialOperationEntity>> GetOperationsAsync(
        Guid walletId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}

/// <summary>Server-side aggregated income/expense totals for a report range.</summary>
/// <param name="TotalIncome">Sum of <see cref="OperationKind.Income"/> amounts.</param>
/// <param name="TotalExpense">Sum of <see cref="OperationKind.Expense"/> amounts.</param>
public readonly record struct ReportTotals(decimal TotalIncome, decimal TotalExpense);
