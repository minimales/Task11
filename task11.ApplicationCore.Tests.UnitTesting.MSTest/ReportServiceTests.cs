using task11.ApplicationCore.Models;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.ApplicationCore.Services;
using task11.ApplicationCore.Entities;
using task11.ApplicationCore.Entities.Enums;
using task11.Infrastructure.Persistence;
using task11.Infrastructure.Repositories;

[TestClass]
public class ReportServiceTests
{
    private static readonly Guid _walletId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static WalletEntity SharedWallet(string currency) => new()
    {
        Id = _walletId,
        Name = "Test Wallet",
        BaseCurrency = currency,
        OwnerUserId = null
    };

    [TestMethod]
    public async Task Test_ReportService_GetDailyAsync_ComputesIncomeExpenseAndNetTotals()
    {
        WalletEntity wallet = SharedWallet("USD");

        FakeReportRepository reportRepository = new(new ReportTotals(1500m, 400m));

        ReportService service = new(
            reportRepository,
            new FakeWalletRepository(wallet),
            new FakeCurrentUser());

        DailyReportModel request = new() { WalletId = _walletId, Date = new DateTime(2026, 6, 13) };

        ReportModel result = await service.GetDailyAsync(request);

        Assert.AreEqual(1500m, result.TotalIncome);
        Assert.AreEqual(400m, result.TotalExpense);
        Assert.AreEqual(1100m, result.NetResult);
        Assert.AreEqual("USD", result.Currency);
    }

    [TestMethod]
    public async Task Test_ReportService_GetPeriodAsync_NetResultIsIncomeMinusExpense_CanBeNegative()
    {
        WalletEntity wallet = SharedWallet("EUR");

        FakeReportRepository reportRepository = new(new ReportTotals(200m, 750m));

        ReportService service = new(
            reportRepository,
            new FakeWalletRepository(wallet),
            new FakeCurrentUser());

        PeriodReportModel request = new()
        {
            WalletId = _walletId,
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 30)
        };

        ReportModel result = await service.GetPeriodAsync(request);

        Assert.AreEqual(-550m, result.NetResult);
        Assert.AreEqual("EUR", result.Currency);
    }

    [TestMethod]
    public async Task Test_ReportService_GetDailyAsync_PassesUtcRange_DateToDatePlusOne()
    {
        WalletEntity wallet = SharedWallet("UAH");

        FakeReportRepository reportRepository = new(new ReportTotals(0m, 0m));

        ReportService service = new(
            reportRepository,
            new FakeWalletRepository(wallet),
            new FakeCurrentUser());

        await service.GetDailyAsync(new DailyReportModel
        {
            WalletId = _walletId,
            Date = new DateTime(2026, 6, 13, 10, 30, 0, DateTimeKind.Unspecified)
        });

        Assert.AreEqual(new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc), reportRepository.CapturedFrom);
        Assert.AreEqual(DateTimeKind.Utc, reportRepository.CapturedFrom.Kind);
        Assert.AreEqual(new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc), reportRepository.CapturedTo);
        Assert.AreEqual(DateTimeKind.Utc, reportRepository.CapturedTo.Kind);
    }

    [TestMethod]
    public async Task Test_ReportService_GetDailyAsync_ExcludesSoftDeletedOperations_FromTotalsAndOperations()
    {
        InMemoryDbContextFactory factory = new(
            nameof(Test_ReportService_GetDailyAsync_ExcludesSoftDeletedOperations_FromTotalsAndOperations));

        OperationTypeEntity incomeType = new() { Name = "Salary", Kind = OperationKind.Income, WalletId = _walletId };
        OperationTypeEntity expenseType = new() { Name = "Groceries", Kind = OperationKind.Expense, WalletId = _walletId };

        DateTime day = new(2026, 6, 13, 12, 0, 0, DateTimeKind.Utc);

        using (AppDbContext db = factory.CreateDbContext())
        {
            db.OperationTypes.Add(incomeType);
            db.OperationTypes.Add(expenseType);

            db.FinancialOperations.Add(Operation(incomeType, 1000m, day));
            db.FinancialOperations.Add(Operation(expenseType, 300m, day));

            db.FinancialOperations.Add(Operation(incomeType, 9999m, day, isDeleted: true));
            db.FinancialOperations.Add(Operation(expenseType, 8888m, day, isDeleted: true));

            db.SaveChanges();
        }

        WalletEntity wallet = SharedWallet("USD");
        ReportRepository reportRepository = new(factory);

        ReportService service = new(
            reportRepository,
            new FakeWalletRepository(wallet),
            new FakeCurrentUser());

        ReportModel result = await service.GetDailyAsync(new DailyReportModel { WalletId = _walletId, Date = day });

        Assert.AreEqual(1000m, result.TotalIncome);
        Assert.AreEqual(300m, result.TotalExpense);
        Assert.AreEqual(700m, result.NetResult);
        Assert.AreEqual(2, result.Operations.Count);
        Assert.IsFalse(result.Operations.Any(o => o.Amount == 9999m || o.Amount == 8888m));
    }

    [TestMethod]
    public async Task Test_ReportService_GetDailyAsync_OperationWithNullOperationTypeNav_DoesNotThrow_AndUsesSafeDefaults()
    {
        WalletEntity wallet = SharedWallet("USD");

        FinancialOperationEntity orphan = new()
        {
            Id = Guid.NewGuid(),
            OperationTypeId = Guid.NewGuid(),
            OperationType = null!,
            WalletId = _walletId,
            Amount = 42m,
            OccurredAtUtc = new DateTime(2026, 6, 13, 9, 0, 0, DateTimeKind.Utc)
        };

        FakeReportRepository reportRepository = new(
            new ReportTotals(42m, 0m),
            new[] { orphan });

        ReportService service = new(
            reportRepository,
            new FakeWalletRepository(wallet),
            new FakeCurrentUser());

        ReportModel result = await service.GetDailyAsync(
            new DailyReportModel { WalletId = _walletId, Date = new DateTime(2026, 6, 13) });

        Assert.AreEqual(1, result.Operations.Count);
        ReportOperationLineModel line = result.Operations[0];
        Assert.AreEqual(string.Empty, line.OperationTypeName);
        Assert.AreEqual(default(OperationKind), line.Kind);
        Assert.AreEqual(42m, line.Amount);
    }

    private static FinancialOperationEntity Operation(OperationTypeEntity type, decimal amount, DateTime occurredAtUtc, bool isDeleted = false) => new()
    {
        OperationTypeId = type.Id,
        OperationType = type,
        WalletId = _walletId,
        Amount = amount,
        OccurredAtUtc = occurredAtUtc,
        IsDeleted = isDeleted,
        DeletedAtUtc = isDeleted ? occurredAtUtc : null
    };
}
