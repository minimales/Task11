using Microsoft.EntityFrameworkCore;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.Data.Entities;
using task11.Data.Entities.Enums;

namespace task11.ApplicationCore.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IReportRepository"/> over <see cref="FinancialOperationEntity"/>.
/// Aggregation runs as a single grouped <c>SUM(Amount)</c> query; the global soft-delete query
/// filter automatically excludes deleted rows from every read. Each method opens its own context.
/// </summary>
public sealed class ReportRepository : IReportRepository
{
    private readonly DbContextFactory _factory;

    public ReportRepository(DbContextFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <inheritdoc />
    public async Task<ReportTotals> GetTotalsAsync(
        Guid walletId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();

        // SUM(Amount) grouped by OperationType.Kind over the half-open UTC range,
        // translated to a single server-side GROUP BY query.
        var grouped = await ctx.FinancialOperations
            .Where(o => o.WalletId == walletId
                        && o.OccurredAtUtc >= fromUtc
                        && o.OccurredAtUtc < toUtc)
            .GroupBy(o => o.OperationType.Kind)
            .Select(g => new { Kind = g.Key, Total = g.Sum(o => o.Amount) })
            .ToListAsync(cancellationToken);

        decimal income = grouped
            .Where(g => g.Kind == OperationKind.Income)
            .Sum(g => g.Total);

        decimal expense = grouped
            .Where(g => g.Kind == OperationKind.Expense)
            .Sum(g => g.Total);

        return new ReportTotals(income, expense);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FinancialOperationEntity>> GetOperationsAsync(
        Guid walletId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        await using var ctx = _factory.CreateDbContext();

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
