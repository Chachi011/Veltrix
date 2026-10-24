using FluentValidation;
using Veltrix.Application.DTOs.Auth;

namespace Veltrix.Application.Validators;

/// <summary>Reusable rules shared by several validators.</summary>
public static class AuthRules
{
    public const int MinimumAge = 18;

    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(100)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");

    public static IRuleBuilderOptions<T, string> ValidEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().EmailAddress().MaximumLength(256);
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator(TimeProvider time)
    {
        RuleFor(x => x.Email).ValidEmail();

        RuleFor(x => x.Nickname)
            .NotEmpty()
            .Length(3, 20)
            .Matches("^[A-Za-z0-9_]+$").WithMessage("Nickname may contain only letters, digits and underscore.");

        RuleFor(x => x.Password).StrongPassword();

        RuleFor(x => x.DateOfBirth)
            .Must(dob => IsAdult(dob, DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime)))
            .WithMessage($"You must be at least {AuthRules.MinimumAge} years old.");

        RuleFor(x => x.IsAdultConfirmed)
            .Equal(true).WithMessage("You must confirm that you are 18 or older.");
    }

    private static bool IsAdult(DateOnly dateOfBirth, DateOnly today) =>
        dateOfBirth <= today.AddYears(-AuthRules.MinimumAge) && dateOfBirth.Year >= 1900;
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(100);
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
    }
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email).ValidEmail();
        RuleFor(x => x.Code).NotEmpty().Matches("^[0-9]{6}$").WithMessage("Code must be 6 digits.");
        RuleFor(x => x.NewPassword).StrongPassword();
    }
}
