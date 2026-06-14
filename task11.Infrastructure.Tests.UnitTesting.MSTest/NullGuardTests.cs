using Microsoft.Extensions.Caching.Memory;
using task11.Infrastructure.Currency;
using task11.Infrastructure.Persistence;
using task11.Infrastructure.Persistence.Interceptors;
using task11.Infrastructure.Repositories;
using task11.Infrastructure.Time;

[TestClass]
public class NullGuardTests
{
    private const string _inMemoryDb = "null-guard-tests";

    private static DbContextFactory Factory() =>
        new(_inMemoryDb, useInMemory: true, new SystemClock());

    private static IMemoryCache Cache() =>
        new MemoryCache(new MemoryCacheOptions());

    private static FrankfurterClient Frankfurter() =>
        new(new HttpClient());

    private static PrivatBankClient PrivatBank() =>
        new(new HttpClient());

    // ----- DbContextFactory -----

    [TestMethod]
    public void Test_DbContextFactory_Constructor_NullConnectionString_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new DbContextFactory(null!, useInMemory: true, new SystemClock()));
    }

    [TestMethod]
    public void Test_DbContextFactory_Constructor_NullClock_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new DbContextFactory("db", useInMemory: true, null!));
    }

    // ----- Interceptors -----

    [TestMethod]
    public void Test_AuditInterceptor_Constructor_NullClock_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new AuditInterceptor(null!));
    }

    [TestMethod]
    public void Test_SoftDeleteInterceptor_Constructor_NullClock_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new SoftDeleteInterceptor(null!));
    }

    // ----- ModelBuilderExtensions -----

    [TestMethod]
    public void Test_ModelBuilderExtensions_ApplySoftDeleteFilter_NullModelBuilder_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => ModelBuilderExtensions.ApplySoftDeleteFilter(null!));
    }

    // ----- UserRepository -----

    [TestMethod]
    public void Test_UserRepository_Constructor_NullFactory_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new UserRepository(null!));
    }

    [TestMethod]
    public async Task Test_UserRepository_GetByUsernameAsync_NullUsername_ThrowsArgumentNullException()
    {
        UserRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.GetByUsernameAsync(null!));
    }

    [TestMethod]
    public async Task Test_UserRepository_UsernameExistsAsync_NullUsername_ThrowsArgumentNullException()
    {
        UserRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.UsernameExistsAsync(null!));
    }

    [TestMethod]
    public async Task Test_UserRepository_AddAsync_NullUser_ThrowsArgumentNullException()
    {
        UserRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.AddAsync(null!));
    }

    [TestMethod]
    public async Task Test_UserRepository_UpdateAsync_NullUser_ThrowsArgumentNullException()
    {
        UserRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.UpdateAsync(null!));
    }

    [TestMethod]
    public async Task Test_UserRepository_SoftDeleteAsync_NullUser_ThrowsArgumentNullException()
    {
        UserRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.SoftDeleteAsync(null!));
    }

    // ----- WalletRepository -----

    [TestMethod]
    public void Test_WalletRepository_Constructor_NullFactory_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new WalletRepository(null!));
    }

    [TestMethod]
    public async Task Test_WalletRepository_AddAsync_NullWallet_ThrowsArgumentNullException()
    {
        WalletRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.AddAsync(null!));
    }

    [TestMethod]
    public async Task Test_WalletRepository_UpdateAsync_NullWallet_ThrowsArgumentNullException()
    {
        WalletRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.UpdateAsync(null!));
    }

    [TestMethod]
    public async Task Test_WalletRepository_SoftDeleteAsync_NullWallet_ThrowsArgumentNullException()
    {
        WalletRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.SoftDeleteAsync(null!));
    }

    // ----- OperationTypeRepository -----

    [TestMethod]
    public void Test_OperationTypeRepository_Constructor_NullFactory_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new OperationTypeRepository(null!));
    }

    [TestMethod]
    public async Task Test_OperationTypeRepository_NameExistsAsync_NullName_ThrowsArgumentNullException()
    {
        OperationTypeRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.NameExistsAsync(Guid.NewGuid(), null!));
    }

    [TestMethod]
    public async Task Test_OperationTypeRepository_AddAsync_NullType_ThrowsArgumentNullException()
    {
        OperationTypeRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.AddAsync(null!));
    }

    [TestMethod]
    public async Task Test_OperationTypeRepository_UpdateAsync_NullType_ThrowsArgumentNullException()
    {
        OperationTypeRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.UpdateAsync(null!));
    }

    [TestMethod]
    public async Task Test_OperationTypeRepository_SoftDeleteAsync_NullType_ThrowsArgumentNullException()
    {
        OperationTypeRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.SoftDeleteAsync(null!));
    }

    // ----- OperationRepository -----

    [TestMethod]
    public void Test_OperationRepository_Constructor_NullFactory_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new OperationRepository(null!));
    }

    [TestMethod]
    public async Task Test_OperationRepository_AddAsync_NullOperation_ThrowsArgumentNullException()
    {
        OperationRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.AddAsync(null!));
    }

    [TestMethod]
    public async Task Test_OperationRepository_UpdateAsync_NullOperation_ThrowsArgumentNullException()
    {
        OperationRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.UpdateAsync(null!));
    }

    [TestMethod]
    public async Task Test_OperationRepository_SoftDeleteAsync_NullOperation_ThrowsArgumentNullException()
    {
        OperationRepository repository = new(Factory());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => repository.SoftDeleteAsync(null!));
    }

    // ----- ReportRepository -----

    [TestMethod]
    public void Test_ReportRepository_Constructor_NullFactory_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new ReportRepository(null!));
    }

    // ----- FrankfurterClient -----

    [TestMethod]
    public void Test_FrankfurterClient_Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new FrankfurterClient(null!));
    }

    [TestMethod]
    public async Task Test_FrankfurterClient_GetRateAsync_NullFrom_ThrowsArgumentNullException()
    {
        FrankfurterClient client = Frankfurter();
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => client.GetRateAsync(null!, "USD", DateTime.UtcNow));
    }

    [TestMethod]
    public async Task Test_FrankfurterClient_GetRateAsync_NullTo_ThrowsArgumentNullException()
    {
        FrankfurterClient client = Frankfurter();
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => client.GetRateAsync("EUR", null!, DateTime.UtcNow));
    }

    // ----- PrivatBankClient -----

    [TestMethod]
    public void Test_PrivatBankClient_Constructor_NullHttpClient_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new PrivatBankClient(null!));
    }

    [TestMethod]
    public async Task Test_PrivatBankClient_GetRateAsync_NullFrom_ThrowsArgumentNullException()
    {
        PrivatBankClient client = PrivatBank();
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => client.GetRateAsync(null!, "UAH", DateTime.UtcNow));
    }

    [TestMethod]
    public async Task Test_PrivatBankClient_GetRateAsync_NullTo_ThrowsArgumentNullException()
    {
        PrivatBankClient client = PrivatBank();
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => client.GetRateAsync("USD", null!, DateTime.UtcNow));
    }

    // ----- CurrencyConverter -----

    [TestMethod]
    public void Test_CurrencyConverter_Constructor_NullFrankfurter_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new CurrencyConverter(null!, PrivatBank(), Cache(), new SystemClock()));
    }

    [TestMethod]
    public void Test_CurrencyConverter_Constructor_NullPrivatBank_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new CurrencyConverter(Frankfurter(), null!, Cache(), new SystemClock()));
    }

    [TestMethod]
    public void Test_CurrencyConverter_Constructor_NullCache_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new CurrencyConverter(Frankfurter(), PrivatBank(), null!, new SystemClock()));
    }

    [TestMethod]
    public void Test_CurrencyConverter_Constructor_NullClock_ThrowsArgumentNullException()
    {
        Assert.ThrowsException<ArgumentNullException>(
            () => new CurrencyConverter(Frankfurter(), PrivatBank(), Cache(), null!));
    }

    [TestMethod]
    public async Task Test_CurrencyConverter_GetRateAsync_NullFrom_ThrowsArgumentNullException()
    {
        CurrencyConverter converter = new(Frankfurter(), PrivatBank(), Cache(), new SystemClock());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => converter.GetRateAsync(null!, "USD", DateTime.UtcNow));
    }

    [TestMethod]
    public async Task Test_CurrencyConverter_GetRateAsync_NullTo_ThrowsArgumentNullException()
    {
        CurrencyConverter converter = new(Frankfurter(), PrivatBank(), Cache(), new SystemClock());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => converter.GetRateAsync("EUR", null!, DateTime.UtcNow));
    }

    [TestMethod]
    public async Task Test_CurrencyConverter_ConvertAsync_NullFrom_ThrowsArgumentNullException()
    {
        CurrencyConverter converter = new(Frankfurter(), PrivatBank(), Cache(), new SystemClock());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => converter.ConvertAsync(100m, null!, "USD", DateTime.UtcNow));
    }

    [TestMethod]
    public async Task Test_CurrencyConverter_ConvertAsync_NullTo_ThrowsArgumentNullException()
    {
        CurrencyConverter converter = new(Frankfurter(), PrivatBank(), Cache(), new SystemClock());
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => converter.ConvertAsync(100m, "EUR", null!, DateTime.UtcNow));
    }
}
