using FluentValidation;
using Sayiad.Domain.Dtos.AuthDtos;

namespace Sayiad.Domain.Validators;

public class RegisterValidator : AbstractValidator<RegisterRequest>
{
    private static readonly string[] AllowedRoles =
        [nameof(UserRole.Customer), nameof(UserRole.Fisherman), nameof(UserRole.BaitSeller), nameof(UserRole.Auctioneer)];

    public RegisterValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");
        RuleFor(x => x.Password)
            .NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit");
        RuleFor(x => x.Phone).NotEmpty();
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required.")
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
        RuleFor(x => x.Birthdate)
            .NotEmpty().WithMessage("Birthdate is required.")
            .Must(BeAtLeast18).WithMessage("You must be at least 18 years old.");
        RuleFor(x => x.Role)
            .Must(r => AllowedRoles.Contains(r))
            .WithMessage("Role must be one of: Customer, Fisherman, BaitSeller, Auctioneer.");
        RuleFor(x => x.LicenseNumber)
            .NotEmpty().WithMessage("License number is required for Fishermen.")
            .When(x => x.Role == nameof(UserRole.Fisherman));
    }

    private static bool BeAtLeast18(string? birthdate)
    {
        if (string.IsNullOrEmpty(birthdate)) return false;
        if (!DateOnly.TryParse(birthdate, out var parsed)) return false;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - parsed.Year;
        if (parsed > today.AddYears(-age)) age--;
        return age >= 18;
    }
}
