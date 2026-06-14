using Microsoft.EntityFrameworkCore;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.Infrastructure.Persistence;
using task11.ApplicationCore.Entities;
using task11.ApplicationCore.Entities.Enums;

namespace task11.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private class KindTotal
    {
        public OperationKind Kind { get; init; }
        public decimal Total { get; init; }
    }

    private readonly DbContextFactory _factory;

    public ReportRepository(DbContextFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task<ReportTotals> GetTotalsAsync(
        Guid walletId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        await using AppDbContext ctx = _factory.CreateDbContext();

        List<KindTotal> grouped = await ctx.FinancialOperations
            .Where(o => o.WalletId == walletId
                        && o.OccurredAtUtc >= fromUtc
                        && o.OccurredAtUtc < toUtc)
            .GroupBy(o => o.OperationType.Kind)
            .Select(g => new KindTotal { Kind = g.Key, Total = g.Sum(o => o.Amount) })
            .ToListAsync(cancellationToken);

        decimal income = grouped
            .Where(g => g.Kind == OperationKind.Income)
            .Sum(g => g.Total);

        decimal expense = grouped
            .Where(g => g.Kind == OperationKind.Expense)
            .Sum(g => g.Total);

        return new ReportTotals(income, expense);
    }

    public async Task<IReadOnlyList<FinancialOperationEntity>> GetOperationsAsync(
        Guid walletId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        await using AppDbContext ctx = _factory.CreateDbContext();

        return await ctx.FinancialOperations
            .Where(o => o.WalletId == walletId
                        && o.OccurredAtUtc >= fromUtc
                        && o.OccurredAtUtc < toUtc)
            .Include(o => o.OperationType)
            .OrderBy(o => o.OccurredAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
