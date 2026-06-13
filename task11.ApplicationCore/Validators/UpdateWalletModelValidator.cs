using FluentValidation;
using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Validators;

/// <summary>
/// Validates <see cref="UpdateWalletModel"/>: name 1..100; currency required and
/// matching ISO-4217 ^[A-Z]{3}$. Immutability once operations exist is enforced in the service.
/// </summary>
public sealed class UpdateWalletModelValidator : AbstractValidator<UpdateWalletModel>
{
    public UpdateWalletModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must be at most 100 characters.");

        RuleFor(x => x.BaseCurrency)
            .NotEmpty().WithMessage("BaseCurrency is required.")
            .Matches("^[A-Z]{3}$")
            .WithMessage("BaseCurrency must be a 3-letter uppercase ISO-4217 code.");
    }
}
