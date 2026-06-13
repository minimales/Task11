using FluentValidation;
using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Validators;

/// <summary>Validates <see cref="UpdateOperationTypeModel"/>.</summary>
public sealed class UpdateOperationTypeModelValidator : AbstractValidator<UpdateOperationTypeModel>
{
    public UpdateOperationTypeModelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.Kind)
            .IsInEnum();
    }
}
