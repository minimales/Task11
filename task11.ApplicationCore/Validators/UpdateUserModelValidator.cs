using FluentValidation;
using task11.ApplicationCore.Models;

namespace task11.ApplicationCore.Validators;

/// <summary>
/// Validates <see cref="UpdateUserModel"/>: username 3..50 matching the allowed charset,
/// role restricted to {Admin, User}, and password (when supplied) >= 6.
/// </summary>
public sealed class UpdateUserModelValidator : AbstractValidator<UpdateUserModel>
{
    private static readonly string[] _allowedRoles = { "Admin", "User" };

    public UpdateUserModelValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .Length(3, 50).WithMessage("Username must be between 3 and 50 characters.")
            .Matches("^[a-zA-Z0-9_.-]+$")
            .WithMessage("Username may only contain letters, digits, '_', '.' and '-'.");

        // Password is optional on update; validate only when provided.
        RuleFor(x => x.Password!)
            .MinimumLength(6).WithMessage("Password must be at least 6 characters.")
            .When(x => !string.IsNullOrEmpty(x.Password));

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .Must(role => _allowedRoles.Contains(role))
            .WithMessage("Role must be either 'Admin' or 'User'.");
    }
}
