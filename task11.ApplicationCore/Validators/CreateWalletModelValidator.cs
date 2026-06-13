using FluentValidation;
using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Validators;

/// <summary>
/// Validates <see cref="CreateWalletModel"/>: name 1..100; currency, when supplied,
/// must match ISO-4217 ^[A-Z]{3}$ (omit to default to "UAH").
/// </summary>
public sealed class CreateWalletModelValidator : AbstractValidator<CreateWalletModel>
{
    public CreateWalletModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must be at most 100 characters.");

        When(x => !string.IsNullOrWhiteSpace(x.BaseCurrency), () =>
        {
            RuleFor(x => x.BaseCurrency!)
                .Matches("^[A-Z]{3}$")
                .WithMessage("BaseCurrency must be a 3-letter uppercase ISO-4217 code.");
        });
    }
}
