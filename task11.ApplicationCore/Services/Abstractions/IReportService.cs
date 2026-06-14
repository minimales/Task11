using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Services.Abstractions;

public interface IReportService
{
    Task<ReportModel> GetDailyAsync(DailyReportModel request, CancellationToken cancellationToken = default);

    Task<ReportModel> GetPeriodAsync(PeriodReportModel request, CancellationToken cancellationToken = default);
}
