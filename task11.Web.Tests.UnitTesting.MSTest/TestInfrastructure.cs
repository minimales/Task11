using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Services.Abstractions;

internal static class ControllerTestExtensions
{
    public static T WithTestContext<T>(this T controller) where T : ControllerBase
    {
        ArgumentNullException.ThrowIfNull(controller);

        HttpContext httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }
}

internal class FakeAuthService : IAuthService
{
    private readonly string _validUsername;
    private readonly string _validPassword;
    private readonly AuthTokenModel _token;

    public LoginModel? LastRequest { get; private set; }

    public FakeAuthService(string validUsername, string validPassword, AuthTokenModel token)
    {
        _validUsername = validUsername;
        _validPassword = validPassword;
        _token = token;
    }

    public Task<AuthTokenModel?> LoginAsync(LoginModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        LastRequest = request;
        bool ok = request.Username == _validUsername && request.Password == _validPassword;
        return Task.FromResult<AuthTokenModel?>(ok ? _token : null);
    }
}

internal class FakeUserService : IUserService
{
    private readonly IReadOnlyList<UserModel> _users;

    public FakeUserService(IReadOnlyList<UserModel>? users = null)
        => _users = users ?? Array.Empty<UserModel>();

    public Task<IReadOnlyList<UserModel>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_users);

    public Task<UserModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(new UserModel { Id = id });

    public Task<UserModel> CreateAsync(CreateUserModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(new UserModel { Id = Guid.NewGuid(), Username = request.Username });
    }

    public Task<UserModel> UpdateAsync(Guid id, UpdateUserModel request, CancellationToken cancellationToken = default)
        => Task.FromResult(new UserModel { Id = id });

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

internal class FakeReportService : IReportService
{
    private readonly ReportModel _report;

    public DailyReportModel? LastDailyRequest { get; private set; }
    public PeriodReportModel? LastPeriodRequest { get; private set; }

    public FakeReportService(ReportModel report) => _report = report;

    public Task<ReportModel> GetDailyAsync(DailyReportModel request, CancellationToken cancellationToken = default)
    {
        LastDailyRequest = request;
        return Task.FromResult(_report);
    }

    public Task<ReportModel> GetPeriodAsync(PeriodReportModel request, CancellationToken cancellationToken = default)
    {
        LastPeriodRequest = request;
        return Task.FromResult(_report);
    }
}

internal class FakeWalletService : IWalletService
{
    private const string _defaultBaseCurrency = "UAH";

    private readonly IReadOnlyList<WalletModel> _accessible;
    private readonly WalletModel _single;

    public CreateWalletModel? LastCreateRequest { get; private set; }

    public FakeWalletService(IReadOnlyList<WalletModel>? accessible = null, WalletModel? single = null)
    {
        _accessible = accessible ?? Array.Empty<WalletModel>();
        _single = single ?? new WalletModel { Id = Guid.NewGuid() };
    }

    public Task<IReadOnlyList<WalletModel>> GetAccessibleAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_accessible);

    public Task<WalletModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_single);

    public Task<WalletModel> CreateAsync(CreateWalletModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        LastCreateRequest = request;
        return Task.FromResult(new WalletModel
        {
            Id = _single.Id,
            Name = request.Name,
            BaseCurrency = string.IsNullOrEmpty(request.BaseCurrency) ? _defaultBaseCurrency : request.BaseCurrency!,
        });
    }

    public Task<WalletModel> UpdateAsync(Guid id, UpdateWalletModel request, CancellationToken cancellationToken = default)
        => Task.FromResult(_single);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<task11.ApplicationCore.Entities.WalletEntity> EnsureCanAccessAsync(Guid walletId, CancellationToken cancellationToken = default)
        => Task.FromResult(new task11.ApplicationCore.Entities.WalletEntity { Id = walletId });
}

internal class FakeOperationService : IOperationService
{
    private readonly IReadOnlyList<OperationModel> _byWallet;
    private readonly OperationModel _single;

    public CreateOperationModel? LastCreateRequest { get; private set; }
    public Guid? LastDeletedId { get; private set; }

    public FakeOperationService(IReadOnlyList<OperationModel>? byWallet = null, OperationModel? single = null)
    {
        _byWallet = byWallet ?? Array.Empty<OperationModel>();
        _single = single ?? new OperationModel { Id = Guid.NewGuid() };
    }

    public Task<IReadOnlyList<OperationModel>> GetByWalletAsync(Guid walletId, CancellationToken cancellationToken = default)
        => Task.FromResult(_byWallet);

    public Task<OperationModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_single);

    public Task<OperationModel> CreateAsync(CreateOperationModel request, CancellationToken cancellationToken = default)
    {
        LastCreateRequest = request;
        return Task.FromResult(_single);
    }

    public Task<OperationModel> UpdateAsync(Guid id, UpdateOperationModel request, CancellationToken cancellationToken = default)
        => Task.FromResult(_single);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        LastDeletedId = id;
        return Task.CompletedTask;
    }
}
