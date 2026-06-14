using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using task11.ApplicationCore;
using task11.ApplicationCore.Auth;
using task11.ApplicationCore.Currency;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.ApplicationCore.Services;
using task11.ApplicationCore.Services.Abstractions;
using task11.ApplicationCore.Entities;

[TestClass]
public class NullGuardTests
{
    private static WalletEntity AnyWallet() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Any",
        BaseCurrency = "UAH",
        OwnerUserId = null
    };

    // ---------- AuthService ----------

    [TestMethod]
    public void Test_AuthService_Constructor_NullUsers_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new AuthService(null!, new PasswordHasher(), CreateTokenGenerator(), new FakeLogger<AuthService>()));
    }

    [TestMethod]
    public void Test_AuthService_Constructor_NullPasswordHasher_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new AuthService(new FakeUserRepository(), null!, CreateTokenGenerator(), new FakeLogger<AuthService>()));
    }

    [TestMethod]
    public void Test_AuthService_Constructor_NullTokenGenerator_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new AuthService(new FakeUserRepository(), new PasswordHasher(), null!, new FakeLogger<AuthService>()));
    }

    [TestMethod]
    public void Test_AuthService_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new AuthService(new FakeUserRepository(), new PasswordHasher(), CreateTokenGenerator(), null!));
    }

    [TestMethod]
    public async Task Test_AuthService_LoginAsync_NullRequest_ThrowsArgumentNullException()
    {
        AuthService service = new(
            new FakeUserRepository(), new PasswordHasher(), CreateTokenGenerator(), new FakeLogger<AuthService>());

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.LoginAsync(null!));
    }

    // ---------- UserService ----------

    [TestMethod]
    public void Test_UserService_Constructor_NullUsers_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new UserService(null!, new PasswordHasher()));
    }

    [TestMethod]
    public void Test_UserService_Constructor_NullPasswordHasher_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new UserService(new FakeUserRepository(), null!));
    }

    [TestMethod]
    public async Task Test_UserService_CreateAsync_NullRequest_ThrowsArgumentNullException()
    {
        UserService service = new(new FakeUserRepository(), new PasswordHasher());

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.CreateAsync(null!));
    }

    [TestMethod]
    public async Task Test_UserService_UpdateAsync_NullRequest_ThrowsArgumentNullException()
    {
        UserService service = new(new FakeUserRepository(), new PasswordHasher());

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => service.UpdateAsync(Guid.NewGuid(), null!));
    }

    // ---------- WalletService ----------

    [TestMethod]
    public void Test_WalletService_Constructor_NullWallets_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new WalletService(null!, new FakeCurrentUser()));
    }

    [TestMethod]
    public void Test_WalletService_Constructor_NullCurrentUser_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new WalletService(new FakeWalletRepository(AnyWallet()), null!));
    }

    [TestMethod]
    public async Task Test_WalletService_CreateAsync_NullRequest_ThrowsArgumentNullException()
    {
        WalletService service = new(new FakeWalletRepository(AnyWallet()), new FakeCurrentUser());

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.CreateAsync(null!));
    }

    [TestMethod]
    public async Task Test_WalletService_UpdateAsync_NullRequest_ThrowsArgumentNullException()
    {
        WalletService service = new(new FakeWalletRepository(AnyWallet()), new FakeCurrentUser());

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => service.UpdateAsync(Guid.NewGuid(), null!));
    }

    // ---------- OperationTypeService ----------

    [TestMethod]
    public void Test_OperationTypeService_Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new OperationTypeService(null!, new FakeWalletService(AnyWallet())));
    }

    [TestMethod]
    public void Test_OperationTypeService_Constructor_NullWalletService_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new OperationTypeService(new FakeOperationTypeRepository(), null!));
    }

    [TestMethod]
    public async Task Test_OperationTypeService_CreateAsync_NullRequest_ThrowsArgumentNullException()
    {
        OperationTypeService service = new(
            new FakeOperationTypeRepository(), new FakeWalletService(AnyWallet()));

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => service.CreateAsync(Guid.NewGuid(), null!));
    }

    [TestMethod]
    public async Task Test_OperationTypeService_UpdateAsync_NullRequest_ThrowsArgumentNullException()
    {
        OperationTypeService service = new(
            new FakeOperationTypeRepository(), new FakeWalletService(AnyWallet()));

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => service.UpdateAsync(Guid.NewGuid(), null!));
    }

    // ---------- OperationService ----------

    [TestMethod]
    public void Test_OperationService_Constructor_NullOperations_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new OperationService(null!, new FakeWalletService(AnyWallet()), new FakeCurrencyConverter(0m, 0m)));
    }

    [TestMethod]
    public void Test_OperationService_Constructor_NullWallets_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new OperationService(new FakeOperationRepository(), null!, new FakeCurrencyConverter(0m, 0m)));
    }

    [TestMethod]
    public void Test_OperationService_Constructor_NullCurrencyConverter_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new OperationService(new FakeOperationRepository(), new FakeWalletService(AnyWallet()), null!));
    }

    [TestMethod]
    public async Task Test_OperationService_CreateAsync_NullRequest_ThrowsArgumentNullException()
    {
        OperationService service = new(
            new FakeOperationRepository(), new FakeWalletService(AnyWallet()), new FakeCurrencyConverter(0m, 0m));

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.CreateAsync(null!));
    }

    [TestMethod]
    public async Task Test_OperationService_UpdateAsync_NullRequest_ThrowsArgumentNullException()
    {
        OperationService service = new(
            new FakeOperationRepository(), new FakeWalletService(AnyWallet()), new FakeCurrencyConverter(0m, 0m));

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => service.UpdateAsync(Guid.NewGuid(), null!));
    }

    // ---------- ReportService ----------

    [TestMethod]
    public void Test_ReportService_Constructor_NullReportRepository_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new ReportService(null!, new FakeWalletRepository(AnyWallet()), new FakeCurrentUser()));
    }

    [TestMethod]
    public void Test_ReportService_Constructor_NullWalletRepository_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new ReportService(new FakeReportRepository(new ReportTotals(0m, 0m)), null!, new FakeCurrentUser()));
    }

    [TestMethod]
    public void Test_ReportService_Constructor_NullCurrentUser_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new ReportService(
                new FakeReportRepository(new ReportTotals(0m, 0m)),
                new FakeWalletRepository(AnyWallet()),
                null!));
    }

    [TestMethod]
    public async Task Test_ReportService_GetDailyAsync_NullRequest_ThrowsArgumentNullException()
    {
        ReportService service = new(
            new FakeReportRepository(new ReportTotals(0m, 0m)),
            new FakeWalletRepository(AnyWallet()),
            new FakeCurrentUser());

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.GetDailyAsync(null!));
    }

    [TestMethod]
    public async Task Test_ReportService_GetPeriodAsync_NullRequest_ThrowsArgumentNullException()
    {
        ReportService service = new(
            new FakeReportRepository(new ReportTotals(0m, 0m)),
            new FakeWalletRepository(AnyWallet()),
            new FakeCurrentUser());

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => service.GetPeriodAsync(null!));
    }

    // ---------- JwtTokenGenerator ----------

    [TestMethod]
    public void Test_JwtTokenGenerator_Constructor_NullSettings_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new JwtTokenGenerator(null!, new FixedClock(DateTime.UtcNow)));
    }

    [TestMethod]
    public void Test_JwtTokenGenerator_Constructor_NullClock_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new JwtTokenGenerator(Options.Create(new JwtSettings()), null!));
    }

    [TestMethod]
    public void Test_JwtTokenGenerator_Generate_NullUser_ThrowsArgumentNullException()
    {
        JwtTokenGenerator generator = CreateTokenGenerator();

        Assert.ThrowsException<ArgumentNullException>(() => generator.Generate(null!));
    }

    // ---------- PasswordHasher ----------

    [TestMethod]
    public void Test_PasswordHasher_Hash_NullPassword_ThrowsArgumentNullException()
    {
        PasswordHasher hasher = new();

        Assert.ThrowsException<ArgumentNullException>(() => hasher.Hash(null!));
    }

    // ---------- helpers ----------

    private static JwtTokenGenerator CreateTokenGenerator() =>
        new(
            Options.Create(new JwtSettings { Secret = "this-is-a-test-secret-key-at-least-32-bytes-long" }),
            new FixedClock(new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc)));
}

internal class FakeUserRepository : IUserRepository
{
    public Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<UserEntity?>(null);

    public Task<IReadOnlyList<UserEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<UserEntity>>(Array.Empty<UserEntity>());

    public Task<UserEntity?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => Task.FromResult<UserEntity?>(null);

    public Task<bool> UsernameExistsAsync(string username, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task AddAsync(UserEntity user, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task UpdateAsync(UserEntity user, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SoftDeleteAsync(UserEntity user, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

internal class FakeOperationTypeRepository : IOperationTypeRepository
{
    public Task<OperationTypeEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<OperationTypeEntity?>(null);

    public Task<IReadOnlyList<OperationTypeEntity>> ListByWalletAsync(Guid walletId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<OperationTypeEntity>>(Array.Empty<OperationTypeEntity>());

    public Task<bool> NameExistsAsync(Guid walletId, string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task AddAsync(OperationTypeEntity type, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task UpdateAsync(OperationTypeEntity type, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SoftDeleteAsync(OperationTypeEntity type, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

internal class FakeLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => false;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }
}
