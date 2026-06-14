using System.Globalization;
using FluentValidation;
using task11.ApplicationCore.Currency;
using task11.ApplicationCore.Models;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.ApplicationCore.Services.Abstractions;
using task11.ApplicationCore.Entities;

namespace task11.ApplicationCore.Services;

public class OperationService : IOperationService
{
    private const decimal _maxAmount = 1_000_000_000m;

    private readonly IOperationRepository _operations;
    private readonly IWalletService _wallets;
    private readonly ICurrencyConverter _currencyConverter;

    public OperationService(
        IOperationRepository operations,
        IWalletService wallets,
        ICurrencyConverter currencyConverter)
    {
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(wallets);
        ArgumentNullException.ThrowIfNull(currencyConverter);

        _operations = operations;
        _wallets = wallets;
        _currencyConverter = currencyConverter;
    }

    public async Task<IReadOnlyList<OperationModel>> GetByWalletAsync(
        Guid walletId,
        CancellationToken cancellationToken = default)
    {
        WalletEntity wallet = await _wallets.EnsureCanAccessAsync(walletId, cancellationToken);

        IReadOnlyList<FinancialOperationEntity> operations = await _operations.ListByWalletAsync(walletId, cancellationToken);
        return operations.Select(o => Map(o, wallet.BaseCurrency)).ToList();
    }

    public async Task<OperationModel> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        FinancialOperationEntity operation = await _operations.GetWithTypeAsync(id, cancellationToken)
                        ?? throw new NotFoundException("Operation", id);

        WalletEntity wallet = await _wallets.EnsureCanAccessAsync(operation.WalletId, cancellationToken);
        return Map(operation, wallet.BaseCurrency);
    }

    public async Task<OperationModel> CreateAsync(
        CreateOperationModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        WalletEntity wallet = await _wallets.EnsureCanAccessAsync(request.WalletId, cancellationToken);

        OperationTypeEntity type = await _operations.GetTypeForWalletAsync(request.TypeId, wallet.Id, cancellationToken)
                   ?? throw new NotFoundException(
                       $"Operation type '{request.TypeId}' was not found in wallet '{wallet.Id}'.");

        DateTime occurredAtUtc = ToUtc(request.Date);

        (decimal amount, string note) = await ApplyConversionAsync(
            request.Amount,
            request.TransactionCurrency,
            wallet.BaseCurrency,
            occurredAtUtc,
            request.Note,
            cancellationToken);

        FinancialOperationEntity operation = new FinancialOperationEntity
        {
            OperationTypeId = type.Id,
            WalletId = wallet.Id,
            Amount = amount,
            OccurredAtUtc = occurredAtUtc,
            Note = note
        };

        await _operations.AddAsync(operation, cancellationToken);

        operation.OperationType = type;
        return Map(operation, wallet.BaseCurrency);
    }

    public async Task<OperationModel> UpdateAsync(
        Guid id,
        UpdateOperationModel request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        FinancialOperationEntity operation = await _operations.GetWithTypeAsync(id, cancellationToken)
                        ?? throw new NotFoundException("Operation", id);

        WalletEntity wallet = await _wallets.EnsureCanAccessAsync(operation.WalletId, cancellationToken);

        OperationTypeEntity type = await _operations.GetTypeForWalletAsync(request.TypeId, wallet.Id, cancellationToken)
                   ?? throw new NotFoundException(
                       $"Operation type '{request.TypeId}' was not found in wallet '{wallet.Id}'.");

        DateTime occurredAtUtc = ToUtc(request.Date);

        (decimal amount, string note) = await ApplyConversionAsync(
            request.Amount,
            request.TransactionCurrency,
            wallet.BaseCurrency,
            occurredAtUtc,
            request.Note,
            cancellationToken);

        operation.OperationTypeId = type.Id;
        operation.Amount = amount;
        operation.OccurredAtUtc = occurredAtUtc;
        operation.Note = note;

        operation.OperationType = null!;

        await _operations.UpdateAsync(operation, cancellationToken);

        operation.OperationType = type;
        return Map(operation, wallet.BaseCurrency);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        FinancialOperationEntity operation = await _operations.GetByIdAsync(id, cancellationToken)
                        ?? throw new NotFoundException("Operation", id);

        await _wallets.EnsureCanAccessAsync(operation.WalletId, cancellationToken);

        await _operations.SoftDeleteAsync(operation, cancellationToken);
    }

    private async Task<(decimal Amount, string? Note)> ApplyConversionAsync(
        decimal originalAmount,
        string? transactionCurrency,
        string baseCurrency,
        DateTime occurredAtUtc,
        string? userNote,
        CancellationToken cancellationToken)
    {

        if (string.IsNullOrWhiteSpace(transactionCurrency)
            || string.Equals(transactionCurrency, baseCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return (originalAmount, userNote);
        }

        string tx = transactionCurrency.ToUpperInvariant();
        (decimal converted, decimal rate) = await _currencyConverter.ConvertAsync(
            originalAmount,
            tx,
            baseCurrency,
            occurredAtUtc.Date,
            cancellationToken);

        if (converted > _maxAmount)
        {
            throw new ValidationException(
                $"The converted amount ({converted:0.##} {baseCurrency}) exceeds the maximum allowed value of {_maxAmount:0.##}.");
        }

        string audit = string.Format(
            CultureInfo.InvariantCulture,
            "[Original: {0:0.##} {1} @ {2:0.######} on {3:yyyy-MM-dd} → {4:0.00} {5}]",
            originalAmount, tx, rate, occurredAtUtc.Date, converted, baseCurrency);

        string note = string.IsNullOrWhiteSpace(userNote) ? audit : $"{userNote} {audit}";
        return (converted, note);
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static OperationModel Map(FinancialOperationEntity operation, string currency) => new()
    {
        Id = operation.Id,
        WalletId = operation.WalletId,
        TypeId = operation.OperationTypeId,
        TypeName = operation.OperationType?.Name ?? string.Empty,
        Kind = operation.OperationType?.Kind ?? default,
        Amount = operation.Amount,
        Currency = currency,
        OccurredAtUtc = operation.OccurredAtUtc,
        Note = operation.Note,
        CreatedAtUtc = operation.CreatedAtUtc,
        UpdatedAtUtc = operation.UpdatedAtUtc
    };
}
