using FluentValidation;
using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Validators;

public class UpdateWalletModelValidator : AbstractValidator<UpdateWalletModel>
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
