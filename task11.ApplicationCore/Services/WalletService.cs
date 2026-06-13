using task11.ApplicationCore.Auth;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.ApplicationCore.Services.Abstractions;
using task11.Data.Entities;

namespace task11.ApplicationCore.Services;

/// <summary>
/// Implements wallet use-cases with service-layer ownership isolation.
/// A wallet is accessible when it is shared (no owner), owned by the caller, or the caller
/// is an admin. The base currency is immutable once the wallet has operations.
/// </summary>
public sealed class WalletService : IWalletService
{
    private const string _defaultCurrency = "UAH";

    private readonly IWalletRepository _wallets;
    private readonly ICurrentUser _currentUser;

    public WalletService(IWalletRepository wallets, ICurrentUser currentUser)
    {
        _wallets = wallets;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WalletModel>> GetAccessibleAsync(CancellationToken cancellationToken = default)
    {
        var wallets = await _wallets.ListAccessibleAsync(
            _currentUser.UserId, _currentUser.IsAdmin, cancellationToken);

        return wallets.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<WalletModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wallet = await EnsureCanAccessAsync(id, cancellationToken);
        return Map(wallet);
    }

    /// <inheritdoc />
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
            // POST always creates a PERSONAL wallet owned by the current user.
            OwnerUserId = _currentUser.UserId
        };

        await _wallets.AddAsync(wallet, cancellationToken);

        return Map(wallet);
    }

    /// <inheritdoc />
    public async Task<WalletModel> UpdateAsync(Guid id, UpdateWalletModel request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wallet = await EnsureCanAccessAsync(id, cancellationToken);

        var newCurrency = request.BaseCurrency.Trim().ToUpperInvariant();

        if (!string.Equals(newCurrency, wallet.BaseCurrency, StringComparison.OrdinalIgnoreCase))
        {
            // BaseCurrency is immutable once any operation has been recorded against the wallet.
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

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var wallet = await EnsureCanAccessAsync(id, cancellationToken);

        await _wallets.SoftDeleteAsync(wallet, cancellationToken);
    }

    /// <inheritdoc />
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

    /// <summary>
    /// Access rule: shared wallet (no owner) OR owned by the caller OR the caller is an admin.
    /// </summary>
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
