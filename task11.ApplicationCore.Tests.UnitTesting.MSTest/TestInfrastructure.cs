using task11.ApplicationCore;
using task11.ApplicationCore.Auth;
using task11.ApplicationCore.Currency;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.ApplicationCore.Services.Abstractions;
using task11.Data;
using task11.Data.Entities;

public class InMemoryDbContextFactory : DbContextFactory
{
    private readonly string _databaseName;
    private readonly IClock _clock;

    public InMemoryDbContextFactory(string databaseName)
        : this(databaseName, new FixedClock(new DateTime(2026, 6, 13, 0, 0, 0, DateTimeKind.Utc)))
    {
    }

    public InMemoryDbContextFactory(string databaseName, IClock clock)
        : base(databaseName, useInMemory: true, clock)
    {
        _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public override AppDbContext CreateDbContext()
    {
        return new AppDbContext(_databaseName, useInMemory: true, _clock);
    }
}

internal class FixedClock : IClock
{
    public DateTime UtcNow { get; }

    public FixedClock(DateTime utcNow) => UtcNow = utcNow;
}

internal class FakeCurrencyConverter : ICurrencyConverter
{
    private readonly decimal _converted;
    private readonly decimal _rate;

    public int ConvertCallCount { get; private set; }
    public int GetRateCallCount { get; private set; }
    public decimal? LastAmount { get; private set; }
    public string? LastFrom { get; private set; }
    public string? LastTo { get; private set; }
    public DateTime? LastDate { get; private set; }

    public FakeCurrencyConverter(decimal converted, decimal rate)
    {
        _converted = converted;
        _rate = rate;
    }

    public Task<decimal> GetRateAsync(string from, string to, DateTime date, CancellationToken cancellationToken = default)
    {
        GetRateCallCount++;
        return Task.FromResult(_rate);
    }

    public Task<(decimal Converted, decimal Rate)> ConvertAsync(
        decimal amount,
        string from,
        string to,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        ConvertCallCount++;
        LastAmount = amount;
        LastFrom = from;
        LastTo = to;
        LastDate = date;
        return Task.FromResult((_converted, _rate));
    }
}

internal class FakeWalletService : IWalletService
{
    private readonly WalletEntity _wallet;

    public int EnsureCanAccessCallCount { get; private set; }

    public FakeWalletService(WalletEntity wallet) => _wallet = wallet;

    public Task<WalletEntity> EnsureCanAccessAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        EnsureCanAccessCallCount++;
        return Task.FromResult(_wallet);
    }

    public Task<IReadOnlyList<WalletModel>> GetAccessibleAsync(CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<WalletModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<WalletModel> CreateAsync(CreateWalletModel request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<WalletModel> UpdateAsync(Guid id, UpdateWalletModel request, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}

internal class FakeOperationRepository : IOperationRepository
{
    private readonly List<FinancialOperationEntity> _operations = new();
    private readonly List<OperationTypeEntity> _types = new();

    public int AddCallCount { get; private set; }
    public int SoftDeleteCallCount { get; private set; }
    public FinancialOperationEntity? LastAdded { get; private set; }

    public void SeedType(OperationTypeEntity type) => _types.Add(type);

    public Task<FinancialOperationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_operations.FirstOrDefault(o => o.Id == id && !o.IsDeleted));

    public Task<FinancialOperationEntity?> GetWithTypeAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_operations.FirstOrDefault(o => o.Id == id && !o.IsDeleted));

    public Task<IReadOnlyList<FinancialOperationEntity>> ListByWalletAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FinancialOperationEntity> result = _operations
            .Where(o => o.WalletId == walletId && !o.IsDeleted)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<OperationTypeEntity?> GetTypeForWalletAsync(Guid typeId, Guid walletId, CancellationToken cancellationToken = default)
        => Task.FromResult(_types.FirstOrDefault(t => t.Id == typeId && t.WalletId == walletId));

    public Task AddAsync(FinancialOperationEntity operation, CancellationToken cancellationToken = default)
    {
        AddCallCount++;
        LastAdded = operation;
        _operations.Add(operation);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(FinancialOperationEntity operation, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SoftDeleteAsync(FinancialOperationEntity operation, CancellationToken cancellationToken = default)
    {
        SoftDeleteCallCount++;
        operation.IsDeleted = true;
        return Task.CompletedTask;
    }
}

internal class FakeWalletRepository : IWalletRepository
{
    private readonly WalletEntity _wallet;

    public FakeWalletRepository(WalletEntity wallet) => _wallet = wallet;

    public Task<WalletEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult<WalletEntity?>(id == _wallet.Id ? _wallet : null);

    public Task<IReadOnlyList<WalletEntity>> ListAccessibleAsync(Guid? userId, bool isAdmin, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task<bool> HasOperationsAsync(Guid walletId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task AddAsync(WalletEntity wallet, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task UpdateAsync(WalletEntity wallet, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    public Task SoftDeleteAsync(WalletEntity wallet, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}

internal class FakeReportRepository : IReportRepository
{
    private readonly ReportTotals _totals;
    private readonly IReadOnlyList<FinancialOperationEntity> _operations;

    public DateTime CapturedFrom { get; private set; }
    public DateTime CapturedTo { get; private set; }

    public FakeReportRepository(ReportTotals totals, IReadOnlyList<FinancialOperationEntity>? operations = null)
    {
        _totals = totals;
        _operations = operations ?? Array.Empty<FinancialOperationEntity>();
    }

    public Task<ReportTotals> GetTotalsAsync(Guid walletId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        CapturedFrom = fromUtc;
        CapturedTo = toUtc;
        return Task.FromResult(_totals);
    }

    public Task<IReadOnlyList<FinancialOperationEntity>> GetOperationsAsync(Guid walletId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
        => Task.FromResult(_operations);
}

internal class FakeCurrentUser : ICurrentUser
{
    public Guid? UserId { get; }
    public string? Role => IsAdmin ? "Admin" : null;
    public bool IsAdmin { get; }
    public bool IsAuthenticated => UserId is not null;

    public FakeCurrentUser(Guid? userId = null, bool isAdmin = false)
    {
        UserId = userId;
        IsAdmin = isAdmin;
    }
}
