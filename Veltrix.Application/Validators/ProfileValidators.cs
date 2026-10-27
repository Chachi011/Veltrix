using FluentValidation;
using Veltrix.Application.DTOs.Profile;

namespace Veltrix.Application.Validators;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.Nickname).ValidNickname();
    }
}

public sealed class UpdatePreferencesRequestValidator : AbstractValidator<UpdatePreferencesRequest>
{
    public UpdatePreferencesRequestValidator()
    {
        RuleFor(x => x.Theme).IsInEnum().WithMessage("Choose System, Dark or Light.");
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(100);

        RuleFor(x => x.NewPassword)
            .StrongPassword()
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from the current password.");
    }
}
