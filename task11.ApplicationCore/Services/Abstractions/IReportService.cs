using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Services.Abstractions;

/// <summary>
/// Produces daily and period financial reports for a wallet. Wallet access is enforced
/// before any data is read; totals are computed server-side over UTC date ranges.
/// </summary>
public interface IReportService
{
    /// <summary>Builds a single-day report for the UTC range <c>[date, date+1)</c>.</summary>
    /// <exception cref="NotFoundException">Wallet not found.</exception>
    /// <exception cref="ForbiddenException">Wallet not accessible to the caller.</exception>
    Task<ReportModel> GetDailyAsync(DailyReportModel request, CancellationToken cancellationToken = default);

    /// <summary>Builds a date-range report for the UTC range <c>[startDate, endDate+1)</c>.</summary>
    /// <exception cref="NotFoundException">Wallet not found.</exception>
    /// <exception cref="ForbiddenException">Wallet not accessible to the caller.</exception>
    Task<ReportModel> GetPeriodAsync(PeriodReportModel request, CancellationToken cancellationToken = default);
}
