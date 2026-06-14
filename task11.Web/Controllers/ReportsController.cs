using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Services.Abstractions;

namespace task11.Web.Controllers;

[ApiController]
[Authorize]
[Route("api/reports")]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        ArgumentNullException.ThrowIfNull(reportService);

        _reportService = reportService;
    }

    [HttpGet("daily")]
    [ProducesResponseType(typeof(ReportModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportModel>> GetDaily(
        [FromQuery] DailyReportModel request,
        CancellationToken cancellationToken)
    {
        ReportModel report = await _reportService.GetDailyAsync(request, cancellationToken);
        return Ok(report);
    }

    [HttpGet("period")]
    [ProducesResponseType(typeof(ReportModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportModel>> GetPeriod(
        [FromQuery] PeriodReportModel request,
        CancellationToken cancellationToken)
    {
        ReportModel report = await _reportService.GetPeriodAsync(request, cancellationToken);
        return Ok(report);
    }
}
