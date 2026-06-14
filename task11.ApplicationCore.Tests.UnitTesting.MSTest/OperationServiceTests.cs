using System.Globalization;
using FluentValidation;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Services;
using task11.ApplicationCore.Entities;
using task11.ApplicationCore.Entities.Enums;

[TestClass]
public class OperationServiceTests
{
    private static readonly Guid _walletId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _typeId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static WalletEntity SharedWallet(string baseCurrency = "UAH") => new()
    {
        Id = _walletId,
        Name = "Shared",
        BaseCurrency = baseCurrency,
        OwnerUserId = null
    };

    private static OperationTypeEntity IncomeType() => new()
    {
        Id = _typeId,
        WalletId = _walletId,
        Name = "Salary",
        Kind = OperationKind.Income
    };

    [TestMethod]
    public async Task Test_OperationService_CreateAsync_ForeignCurrency_ConvertsAmountAndAppendsAuditNote()
    {
        WalletEntity wallet = SharedWallet("UAH");
        DateTime date = new(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        FakeOperationRepository repo = new();
        repo.SeedType(IncomeType());
        FakeWalletService wallets = new(wallet);
        FakeCurrencyConverter fx = new(converted: 4050m, rate: 40.5m);

        OperationService service = new(repo, wallets, fx);

        CreateOperationModel request = new()
        {
            WalletId = _walletId,
            TypeId = _typeId,
            Amount = 100m,
            Date = date,
            Note = "Freelance",
            TransactionCurrency = "EUR"
        };

        OperationModel response = await service.CreateAsync(request);

        Assert.AreEqual(4050m, response.Amount);
        Assert.AreEqual("UAH", response.Currency);
        Assert.IsNotNull(response.Note);
        Assert.IsTrue(response.Note!.StartsWith("Freelance "));
        Assert.IsTrue(response.Note!.Contains("[Original: 100 EUR @ 40.5 on 2024-01-15 → 4050.00 UAH]"));

        Assert.IsNotNull(repo.LastAdded);
        Assert.AreEqual(4050m, repo.LastAdded!.Amount);
        Assert.AreEqual(DateTimeKind.Utc, repo.LastAdded!.OccurredAtUtc.Kind);

        Assert.AreEqual(1, fx.ConvertCallCount);
        Assert.AreEqual(100m, fx.LastAmount);
        Assert.AreEqual("EUR", fx.LastFrom);
        Assert.AreEqual("UAH", fx.LastTo);
        Assert.AreEqual(date.Date, fx.LastDate);
    }

    [DataTestMethod]
    [DataRow("250.50", "Rent", "UAH")]
    [DataRow("99", null, null)]
    public async Task Test_OperationService_CreateAsync_NoForeignConversion_DoesNotCallFx(
        string amount, string? note, string? transactionCurrency)
    {
        decimal expectedAmount = decimal.Parse(amount, CultureInfo.InvariantCulture);
        WalletEntity wallet = SharedWallet("UAH");
        DateTime date = new(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        FakeOperationRepository repo = new();
        repo.SeedType(IncomeType());
        FakeWalletService wallets = new(wallet);
        FakeCurrencyConverter fx = new(converted: 0m, rate: 0m);

        OperationService service = new(repo, wallets, fx);

        CreateOperationModel request = new()
        {
            WalletId = _walletId,
            TypeId = _typeId,
            Amount = expectedAmount,
            Date = date,
            Note = note,
            TransactionCurrency = transactionCurrency
        };

        OperationModel response = await service.CreateAsync(request);

        Assert.AreEqual(expectedAmount, response.Amount);
        Assert.AreEqual(note, response.Note);
        Assert.AreEqual(0, fx.ConvertCallCount);
    }

    [TestMethod]
    public async Task Test_OperationService_CreateAsync_ForeignCurrencyNoUserNote_NoteIsAuditOnly()
    {
        WalletEntity wallet = SharedWallet("UAH");
        DateTime date = new(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        FakeOperationRepository repo = new();
        repo.SeedType(IncomeType());
        FakeWalletService wallets = new(wallet);
        FakeCurrencyConverter fx = new(converted: 4050m, rate: 40.5m);

        OperationService service = new(repo, wallets, fx);

        CreateOperationModel request = new()
        {
            WalletId = _walletId,
            TypeId = _typeId,
            Amount = 100m,
            Date = date,
            Note = null,
            TransactionCurrency = "EUR"
        };

        OperationModel response = await service.CreateAsync(request);

        Assert.IsNotNull(response.Note);
        Assert.AreEqual("[Original: 100 EUR @ 40.5 on 2024-01-15 → 4050.00 UAH]", response.Note);
    }

    [TestMethod]
    public async Task Test_OperationService_CreateAsync_ConvertedAmountExceedsCap_ThrowsValidationException()
    {
        WalletEntity wallet = SharedWallet("UAH");
        DateTime date = new(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        FakeOperationRepository repo = new();
        repo.SeedType(IncomeType());
        FakeWalletService wallets = new(wallet);

        FakeCurrencyConverter fx = new(converted: 5_000_000_000m, rate: 50_000_000m);

        OperationService service = new(repo, wallets, fx);

        CreateOperationModel request = new()
        {
            WalletId = _walletId,
            TypeId = _typeId,
            Amount = 100m,
            Date = date,
            Note = null,
            TransactionCurrency = "EUR"
        };

        await Assert.ThrowsExceptionAsync<ValidationException>(() => service.CreateAsync(request));

        Assert.AreEqual(1, fx.ConvertCallCount);
        Assert.AreEqual(0, repo.AddCallCount);
    }

    [TestMethod]
    public async Task Test_OperationService_DeleteAsync_SoftDeletesAndRowIsHiddenFromSubsequentReads()
    {
        WalletEntity wallet = SharedWallet("UAH");
        Guid operationId = Guid.NewGuid();

        FakeOperationRepository repo = new();
        FakeWalletService wallets = new(wallet);
        FakeCurrencyConverter fx = new(converted: 0m, rate: 0m);

        FinancialOperationEntity operation = new()
        {
            Id = operationId,
            WalletId = _walletId,
            OperationTypeId = _typeId,
            Amount = 10m,
            OccurredAtUtc = DateTime.UtcNow
        };
        await repo.AddAsync(operation);

        OperationService service = new(repo, wallets, fx);

        IReadOnlyList<OperationModel> beforeDelete = await service.GetByWalletAsync(_walletId);
        Assert.AreEqual(1, beforeDelete.Count);

        await service.DeleteAsync(operationId);

        Assert.AreEqual(1, repo.SoftDeleteCallCount);

        IReadOnlyList<OperationModel> afterDelete = await service.GetByWalletAsync(_walletId);
        Assert.AreEqual(0, afterDelete.Count);
    }
}
