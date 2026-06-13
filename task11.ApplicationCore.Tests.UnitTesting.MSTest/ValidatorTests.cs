using FluentValidation.Results;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Validators;

[TestClass]
public class ValidatorTests
{
    private readonly CreateOperationModelValidator _create = new();
    private readonly UpdateOperationModelValidator _update = new();
    private readonly PeriodReportModelValidator _period = new();

    private static CreateOperationModel ValidCreate() => new()
    {
        WalletId = Guid.NewGuid(),
        TypeId = Guid.NewGuid(),
        Amount = 100m,
        Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Note = "ok",
        TransactionCurrency = "USD"
    };

    [TestMethod]
    public void Test_CreateOperationModelValidator_Validate_ValidRequest_Passes()
    {
        Assert.IsTrue(_create.Validate(ValidCreate()).IsValid);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(-0.01)]
    public void Test_CreateOperationModelValidator_Validate_NonPositiveAmount_IsRejected(double amount)
    {
        CreateOperationModel request = ValidCreate();
        request.Amount = (decimal)amount;

        ValidationResult result = _create.Validate(request);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.PropertyName == nameof(CreateOperationModel.Amount)));
    }

    [TestMethod]
    public void Test_CreateOperationModelValidator_Validate_AmountAboveCap_IsRejected()
    {
        CreateOperationModel request = ValidCreate();
        request.Amount = 1_000_000_001m;

        Assert.IsFalse(_create.Validate(request).IsValid);
    }

    [TestMethod]
    public void Test_CreateOperationModelValidator_Validate_AmountWithMoreThanTwoDecimals_IsRejected()
    {
        CreateOperationModel request = ValidCreate();
        request.Amount = 1.234m;

        Assert.IsFalse(_create.Validate(request).IsValid);
    }

    [TestMethod]
    public void Test_CreateOperationModelValidator_Validate_DateTooFarInFuture_IsRejected()
    {
        CreateOperationModel request = ValidCreate();
        request.Date = DateTime.UtcNow.AddDays(2);

        ValidationResult result = _create.Validate(request);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.PropertyName == nameof(CreateOperationModel.Date)));
    }

    [TestMethod]
    public void Test_CreateOperationModelValidator_Validate_DateBefore2000_IsRejected()
    {
        CreateOperationModel request = ValidCreate();
        request.Date = new DateTime(1999, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        Assert.IsFalse(_create.Validate(request).IsValid);
    }

    [TestMethod]
    public void Test_CreateOperationModelValidator_Validate_DateWithinOneDayFuture_Passes()
    {
        CreateOperationModel request = ValidCreate();
        request.Date = DateTime.UtcNow.AddHours(12);

        Assert.IsTrue(_create.Validate(request).IsValid);
    }

    [DataTestMethod]
    [DataRow("US")]
    [DataRow("usd")]
    [DataRow("USDD")]
    [DataRow("U1D")]
    [DataRow("12 ")]
    public void Test_CreateOperationModelValidator_Validate_InvalidCurrencyCode_IsRejected(string currency)
    {
        CreateOperationModel request = ValidCreate();
        request.TransactionCurrency = currency;

        ValidationResult result = _create.Validate(request);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.PropertyName == nameof(CreateOperationModel.TransactionCurrency)));
    }

    [DataTestMethod]
    [DataRow("USD")]
    [DataRow("EUR")]
    [DataRow("UAH")]
    public void Test_CreateOperationModelValidator_Validate_ValidCurrencyCode_Passes(string currency)
    {
        CreateOperationModel request = ValidCreate();
        request.TransactionCurrency = currency;

        Assert.IsTrue(_create.Validate(request).IsValid);
    }

    [TestMethod]
    public void Test_CreateOperationModelValidator_Validate_NullCurrency_Passes()
    {
        CreateOperationModel request = ValidCreate();
        request.TransactionCurrency = null;

        Assert.IsTrue(_create.Validate(request).IsValid);
    }

    [TestMethod]
    public void Test_CreateOperationModelValidator_Validate_NoteTooLong_IsRejected()
    {
        CreateOperationModel request = ValidCreate();
        request.Note = new string('x', 501);

        Assert.IsFalse(_create.Validate(request).IsValid);
    }

    [TestMethod]
    public void Test_UpdateOperationModelValidator_Validate_ZeroAmount_IsRejected()
    {
        UpdateOperationModel request = new()
        {
            TypeId = Guid.NewGuid(),
            Amount = 0m,
            Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        Assert.IsFalse(_update.Validate(request).IsValid);
    }

    [TestMethod]
    public void Test_UpdateOperationModelValidator_Validate_InvalidCurrency_IsRejected()
    {
        UpdateOperationModel request = new()
        {
            TypeId = Guid.NewGuid(),
            Amount = 10m,
            Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            TransactionCurrency = "eur"
        };

        Assert.IsFalse(_update.Validate(request).IsValid);
    }

    [TestMethod]
    public void Test_PeriodReportModelValidator_Validate_ValidPeriod_Passes()
    {
        PeriodReportModel request = new()
        {
            WalletId = Guid.NewGuid(),
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 30)
        };

        Assert.IsTrue(_period.Validate(request).IsValid);
    }

    [TestMethod]
    public void Test_PeriodReportModelValidator_Validate_StartAfterEnd_IsRejected()
    {
        PeriodReportModel request = new()
        {
            WalletId = Guid.NewGuid(),
            StartDate = new DateTime(2026, 6, 30),
            EndDate = new DateTime(2026, 6, 1)
        };

        ValidationResult result = _period.Validate(request);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.PropertyName == nameof(PeriodReportModel.StartDate)));
    }

    [TestMethod]
    public void Test_PeriodReportModelValidator_Validate_SpanExceeds366Days_IsRejected()
    {
        PeriodReportModel request = new()
        {
            WalletId = Guid.NewGuid(),
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2026, 6, 1)
        };

        ValidationResult result = _period.Validate(request);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.PropertyName == nameof(PeriodReportModel.EndDate)));
    }

    [TestMethod]
    public void Test_PeriodReportModelValidator_Validate_EmptyWalletId_IsRejected()
    {
        PeriodReportModel request = new()
        {
            WalletId = Guid.Empty,
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 6, 30)
        };

        ValidationResult result = _period.Validate(request);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.PropertyName == nameof(PeriodReportModel.WalletId)));
    }
}
