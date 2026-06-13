using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Services.Abstractions;

namespace task11.Web.Controllers;

/// <summary>
/// Read-only financial reports. Totals are computed server-side over UTC date ranges;
/// wallet access is enforced in the service layer.
/// </summary>
[ApiController]
[Authorize]
[Route("api/reports")]
[Produces("application/json")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// Returns a single-day report for the UTC range <c>[date, date+1)</c>.
    /// </summary>
    [HttpGet("daily")]
    [ProducesResponseType(typeof(ReportModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportModel>> GetDaily(
        [FromQuery] DailyReportModel request,
        CancellationToken cancellationToken)
    {
        var report = await _reportService.GetDailyAsync(request, cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Returns a date-range report for the UTC range <c>[startDate, endDate+1)</c>.
    /// </summary>
    [HttpGet("period")]
    [ProducesResponseType(typeof(ReportModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportModel>> GetPeriod(
        [FromQuery] PeriodReportModel request,
        CancellationToken cancellationToken)
    {
        var report = await _reportService.GetPeriodAsync(request, cancellationToken);
        return Ok(report);
    }
}
