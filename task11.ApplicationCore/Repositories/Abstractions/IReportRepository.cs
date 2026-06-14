using task11.ApplicationCore.Entities;

namespace task11.ApplicationCore.Repositories.Abstractions;

public interface IReportRepository
{

    Task<ReportTotals> GetTotalsAsync(
        Guid walletId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinancialOperationEntity>> GetOperationsAsync(
        Guid walletId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}

public readonly record struct ReportTotals(decimal TotalIncome, decimal TotalExpense);
