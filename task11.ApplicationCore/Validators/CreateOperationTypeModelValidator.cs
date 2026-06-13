using FluentValidation;
using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Validators;

public class CreateOperationTypeModelValidator : AbstractValidator<CreateOperationTypeModel>
{
    public CreateOperationTypeModelValidator()
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
