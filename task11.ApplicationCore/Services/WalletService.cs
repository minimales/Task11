using task11.ApplicationCore.Auth;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.ApplicationCore.Services.Abstractions;
using task11.ApplicationCore.Entities;

namespace task11.ApplicationCore.Services;

public class WalletService : IWalletService
{
    private const string _defaultCurrency = "UAH";

    private readonly IWalletRepository _wallets;
    private readonly ICurrentUser _currentUser;

    public WalletService(IWalletRepository wallets, ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(wallets);
        ArgumentNullException.ThrowIfNull(currentUser);

        _wallets = wallets;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<WalletModel>> GetAccessibleAsync(CancellationToken cancellationToken = default)
    {
        var wallets = await _wallets.ListAccessibleAsync(
            _currentUser.UserId, _currentUser.IsAdmin, cancellationToken);

        return wallets.Select(Map).ToList();
    }

    public async Task<WalletModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wallet = await EnsureCanAccessAsync(id, cancellationToken);
        return Map(wallet);
    }

    public async Task<WalletModel> CreateAsync(CreateWalletModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currency = string.IsNullOrWhiteSpace(request.BaseCurrency)
            ? _defaultCurrency
            : request.BaseCurrency.Trim().ToUpperInvariant();

        var wallet = new WalletEntity
        {
            Name = request.Name.Trim(),
            BaseCurrency = currency,

            OwnerUserId = _currentUser.UserId
        };

        await _wallets.AddAsync(wallet, cancellationToken);

        return Map(wallet);
    }

    public async Task<WalletModel> UpdateAsync(Guid id, UpdateWalletModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wallet = await EnsureCanAccessAsync(id, cancellationToken);

        var newCurrency = request.BaseCurrency.Trim().ToUpperInvariant();

        if (!string.Equals(newCurrency, wallet.BaseCurrency, StringComparison.OrdinalIgnoreCase))
        {

            if (await _wallets.HasOperationsAsync(wallet.Id, cancellationToken))
            {
                throw new ConflictException(
                    "The wallet base currency cannot be changed once operations exist.");
            }

            wallet.BaseCurrency = newCurrency;
        }

        wallet.Name = request.Name.Trim();

        await _wallets.UpdateAsync(wallet, cancellationToken);

        return Map(wallet);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wallet = await EnsureCanAccessAsync(id, cancellationToken);

        await _wallets.SoftDeleteAsync(wallet, cancellationToken);
    }

    public async Task<WalletEntity> EnsureCanAccessAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        var wallet = await _wallets.GetByIdAsync(walletId, cancellationToken)
            ?? throw new NotFoundException("Wallet", walletId);

        if (!CanAccess(wallet))
        {
            throw new ForbiddenException("You do not have access to this wallet.");
        }

        return wallet;
    }

    private bool CanAccess(WalletEntity wallet) =>
        wallet.OwnerUserId is null
        || wallet.OwnerUserId == _currentUser.UserId
        || _currentUser.IsAdmin;

    private static WalletModel Map(WalletEntity wallet) => new()
    {
        Id = wallet.Id,
        Name = wallet.Name,
        BaseCurrency = wallet.BaseCurrency,
        OwnerUserId = wallet.OwnerUserId,
        CreatedAtUtc = wallet.CreatedAtUtc,
        UpdatedAtUtc = wallet.UpdatedAtUtc
    };
}
