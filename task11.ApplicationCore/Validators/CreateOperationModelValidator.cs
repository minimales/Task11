using FluentValidation;
using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Validators;

public class CreateOperationModelValidator : AbstractValidator<CreateOperationModel>
{
    public CreateOperationModelValidator()
    {
        RuleFor(x => x.WalletId)
            .NotEmpty().WithMessage("WalletId is required.");

        RuleFor(x => x.TypeId)
            .NotEmpty().WithMessage("TypeId is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0m).WithMessage("Amount must be greater than zero.")
            .LessThanOrEqualTo(1_000_000_000m).WithMessage("Amount must not exceed 1,000,000,000.")
            .Must(OperationValidationRules.HasAtMostTwoDecimals)
            .WithMessage("Amount must have at most two decimal places.");

        RuleFor(x => x.Date)
            .Must(d => d != default).WithMessage("Date is required.")
            .Must(OperationValidationRules.IsWithinAllowedWindow)
            .WithMessage("Date must be on or after 2000-01-01 and no more than one day in the future.");

        RuleFor(x => x.TransactionCurrency)
            .Matches(OperationValidationRules.CurrencyPattern)
            .When(x => !string.IsNullOrEmpty(x.TransactionCurrency))
            .WithMessage("TransactionCurrency must be a 3-letter ISO-4217 code (e.g. USD).");

        RuleFor(x => x.Note)
            .MaximumLength(500).WithMessage("Note must be 500 characters or fewer.");
    }
}
