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

public class ReportTotals
{
    public decimal TotalIncome { get; }

    public decimal TotalExpense { get; }

    public ReportTotals(decimal totalIncome, decimal totalExpense)
    {
        TotalIncome = totalIncome;
        TotalExpense = totalExpense;
    }
}
