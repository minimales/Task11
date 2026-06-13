using FluentValidation;
using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Validators;

/// <summary>
/// Validates <see cref="CreateUserModel"/>: username 3..50 matching the allowed charset,
/// password >= 6, and role restricted to {Admin, User}.
/// </summary>
public sealed class CreateUserModelValidator : AbstractValidator<CreateUserModel>
{
    private static readonly string[] _allowedRoles = { "Admin", "User" };

    public CreateUserModelValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .Length(3, 50).WithMessage("Username must be between 3 and 50 characters.")
            .Matches("^[a-zA-Z0-9_.-]+$")
            .WithMessage("Username may only contain letters, digits, '_', '.' and '-'.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => _allowedRoles.Contains(role))
            .WithMessage("Role must be either 'Admin' or 'User'.");
    }
}
