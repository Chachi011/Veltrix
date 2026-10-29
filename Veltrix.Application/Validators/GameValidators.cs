using FluentValidation;
using Veltrix.Application.DTOs.Games;

namespace Veltrix.Application.Validators;

public sealed class SaveGameRequestValidator : AbstractValidator<SaveGameRequest>
{
    public SaveGameRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(3, 100);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .Length(3, 50)
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$").WithMessage("Slug may contain lowercase letters, digits and single dashes (e.g. 'crash-classic').");

        RuleFor(x => x.Description).MaximumLength(2000);

        RuleFor(x => x.MinBet).GreaterThan(0);
        RuleFor(x => x.MaxBet)
            .GreaterThanOrEqualTo(x => x.MinBet).WithMessage("MaxBet must be greater than or equal to MinBet.")
            .LessThanOrEqualTo(1_000_000);

        RuleFor(x => x.BettingDurationSec).InclusiveBetween(3, 120);
        RuleFor(x => x.PauseBetweenRoundsSec).InclusiveBetween(0, 120);
    }
}

public sealed class UpdateGameStatusRequestValidator : AbstractValidator<UpdateGameStatusRequest>
{
    public UpdateGameStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
