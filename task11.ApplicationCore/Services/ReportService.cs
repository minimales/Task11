using task11.ApplicationCore.Auth;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.ApplicationCore.Services.Abstractions;
using task11.ApplicationCore.Entities;

namespace task11.ApplicationCore.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly ICurrentUser _currentUser;

    public ReportService(
        IReportRepository reportRepository,
        IWalletRepository walletRepository,
        ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(reportRepository);
        ArgumentNullException.ThrowIfNull(walletRepository);
        ArgumentNullException.ThrowIfNull(currentUser);

        _reportRepository = reportRepository;
        _walletRepository = walletRepository;
        _currentUser = currentUser;
    }

    public async Task<ReportModel> GetDailyAsync(
        DailyReportModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wallet = await ResolveAccessibleWalletAsync(request.WalletId, cancellationToken);

        var fromUtc = ToUtcDate(request.Date);
        var toUtc = fromUtc.AddDays(1);

        return await BuildReportAsync(wallet, fromUtc, toUtc, cancellationToken);
    }

    public async Task<ReportModel> GetPeriodAsync(
        PeriodReportModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wallet = await ResolveAccessibleWalletAsync(request.WalletId, cancellationToken);

        var fromUtc = ToUtcDate(request.StartDate);
        var toUtc = ToUtcDate(request.EndDate).AddDays(1);

        return await BuildReportAsync(wallet, fromUtc, toUtc, cancellationToken);
    }

    private async Task<WalletEntity> ResolveAccessibleWalletAsync(Guid walletId, CancellationToken cancellationToken)
    {
        var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken)
                     ?? throw new NotFoundException(nameof(WalletEntity), walletId);

        EnsureCanAccess(wallet);
        return wallet;
    }

    private void EnsureCanAccess(WalletEntity wallet)
    {
        bool canAccess = wallet.OwnerUserId is null
                         || wallet.OwnerUserId == _currentUser.UserId
                         || _currentUser.IsAdmin;

        if (!canAccess)
        {
            throw new ForbiddenException();
        }
    }

    private async Task<ReportModel> BuildReportAsync(
        WalletEntity wallet,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var totals = await _reportRepository.GetTotalsAsync(wallet.Id, fromUtc, toUtc, cancellationToken);
        var operations = await _reportRepository.GetOperationsAsync(wallet.Id, fromUtc, toUtc, cancellationToken);

        return new ReportModel
        {
            TotalIncome = totals.TotalIncome,
            TotalExpense = totals.TotalExpense,
            NetResult = totals.TotalIncome - totals.TotalExpense,
            Currency = wallet.BaseCurrency,
            Operations = operations.Select(MapToLine).ToList()
        };
    }

    private static ReportOperationLineModel MapToLine(FinancialOperationEntity operation) => new()
    {
        Id = operation.Id,
        OperationTypeId = operation.OperationTypeId,
        OperationTypeName = operation.OperationType?.Name ?? string.Empty,
        Kind = operation.OperationType!.Kind,
        Amount = operation.Amount,
        OccurredAtUtc = operation.OccurredAtUtc,
        Note = operation.Note
    };

    private static DateTime ToUtcDate(DateTime date)
    {
        var utc = date.Kind switch
        {
            DateTimeKind.Utc => date,
            DateTimeKind.Local => date.ToUniversalTime(),
            _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };

        return DateTime.SpecifyKind(utc.Date, DateTimeKind.Utc);
    }
}
