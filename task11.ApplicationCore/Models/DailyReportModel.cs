namespace task11.ApplicationCore.Models;

/// <summary>
/// Query parameters for a single-day report: <c>GET /api/reports/daily?walletId=&amp;date=</c>.
/// The report covers the UTC range <c>[date, date+1)</c>.
/// </summary>
public sealed class DailyReportModel
{
    /// <summary>The wallet to report on. Access is checked in the service layer.</summary>
    public Guid WalletId { get; set; }

    /// <summary>The report day. The time component is ignored; the range is computed in UTC.</summary>
    public DateTime Date { get; set; }
}
