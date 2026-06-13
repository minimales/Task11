using Microsoft.AspNetCore.Mvc;
using task11.ApplicationCore.Models;
using task11.Web.Controllers;

[TestClass]
public class ReportsControllerTests
{
    private const int _okStatusCode = 200;

    private static readonly Guid _walletId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static ReportModel Report(string currency, decimal income, decimal expense) => new()
    {
        TotalIncome = income,
        TotalExpense = expense,
        NetResult = income - expense,
        Currency = currency,
        Operations = Array.Empty<ReportOperationLineModel>(),
    };

    [TestMethod]
    public async Task Test_ReportsController_GetDaily_ReturnsOkWithIncomeExpenseAndNetTotals()
    {
        FakeReportService reportService = new(Report("USD", income: 1500m, expense: 400m));
        ReportsController controller = new ReportsController(reportService).WithTestContext();

        ActionResult<ReportModel> result = await controller.GetDaily(
            new DailyReportModel { WalletId = _walletId, Date = new DateTime(2026, 6, 13) },
            CancellationToken.None);

        OkObjectResult ok = (OkObjectResult)result.Result!;
        Assert.AreEqual(_okStatusCode, ok.StatusCode);
        ReportModel body = (ReportModel)ok.Value!;
        Assert.AreEqual(1500m, body.TotalIncome);
        Assert.AreEqual(400m, body.TotalExpense);
        Assert.AreEqual(1100m, body.NetResult);
        Assert.AreEqual("USD", body.Currency);
    }

    [TestMethod]
    public async Task Test_ReportsController_GetPeriod_NetResultCanBeNegative()
    {
        FakeReportService reportService = new(Report("EUR", income: 200m, expense: 750m));
        ReportsController controller = new ReportsController(reportService).WithTestContext();

        ActionResult<ReportModel> result = await controller.GetPeriod(
            new PeriodReportModel
            {
                WalletId = _walletId,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 6, 30),
            },
            CancellationToken.None);

        OkObjectResult ok = (OkObjectResult)result.Result!;
        ReportModel body = (ReportModel)ok.Value!;
        Assert.AreEqual(-550m, body.NetResult);
        Assert.AreEqual("EUR", body.Currency);
    }

    [TestMethod]
    public async Task Test_ReportsController_GetDaily_PassesRequestToService()
    {
        FakeReportService reportService = new(Report("UAH", 0m, 0m));
        ReportsController controller = new ReportsController(reportService).WithTestContext();
        DateTime date = new(2026, 6, 13, 10, 30, 0, DateTimeKind.Unspecified);

        await controller.GetDaily(
            new DailyReportModel { WalletId = _walletId, Date = date },
            CancellationToken.None);

        Assert.IsNotNull(reportService.LastDailyRequest);
        Assert.AreEqual(_walletId, reportService.LastDailyRequest!.WalletId);
        Assert.AreEqual(date, reportService.LastDailyRequest.Date);
    }

    [TestMethod]
    public async Task Test_ReportsController_GetPeriod_PassesRequestToService()
    {
        FakeReportService reportService = new(Report("UAH", 0m, 0m));
        ReportsController controller = new ReportsController(reportService).WithTestContext();

        await controller.GetPeriod(
            new PeriodReportModel
            {
                WalletId = _walletId,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 6, 30),
            },
            CancellationToken.None);

        Assert.IsNotNull(reportService.LastPeriodRequest);
        Assert.AreEqual(_walletId, reportService.LastPeriodRequest!.WalletId);
    }
}
