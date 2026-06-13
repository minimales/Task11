namespace task11.ApplicationCore.Models;

/// <summary>
/// Query parameters for a date-range report:
/// <c>GET /api/reports/period?walletId=&amp;startDate=&amp;endDate=</c>.
/// The report covers the UTC range <c>[startDate, endDate+1)</c>.
/// </summary>
public sealed class PeriodReportModel
{
    /// <summary>The wallet to report on. Access is checked in the service layer.</summary>
    public Guid WalletId { get; set; }

    /// <summary>Inclusive first day of the period. Time component ignored; range computed in UTC.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Inclusive last day of the period. Time component ignored; range computed in UTC.</summary>
    public DateTime EndDate { get; set; }
}
