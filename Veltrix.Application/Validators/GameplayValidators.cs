using FluentValidation;
using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Bets;
using Veltrix.Application.DTOs.Fairness;
using Veltrix.Application.DTOs.Rounds;
using Veltrix.Application.DTOs.Wallet;
using Veltrix.Domain.Entities;

namespace Veltrix.Application.Validators;

/// <summary>Shared paging rules; each concrete query validator inherits them.</summary>
public abstract class PageQueryValidator<T> : AbstractValidator<T> where T : PageQuery
{
    protected PageQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageQuery.MaxPageSize);
    }
}

public sealed class RoundHistoryQueryValidator : PageQueryValidator<RoundHistoryQuery>
{
}

public sealed class MyBetsQueryValidator : PageQueryValidator<MyBetsQuery>
{
    public MyBetsQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From).When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("'To' must be after 'From'.");
    }
}

public sealed class TransactionQueryValidator : PageQueryValidator<TransactionQuery>
{
    public TransactionQueryValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From).When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("'To' must be after 'From'.");
    }
}

public sealed class PlaceBetRequestValidator : AbstractValidator<PlaceBetRequest>
{
    public PlaceBetRequestValidator()
    {
        RuleFor(x => x.GameSlug).NotEmpty().MaximumLength(50);

        // Game-specific min/max are checked by the server against the game's settings
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("Amount can have at most 2 decimal places.");

        RuleFor(x => x.AutoCashOutAt)
            .GreaterThanOrEqualTo(Bet.MinAutoCashOut)
            .LessThanOrEqualTo(1_000_000)
            .PrecisionScale(10, 2, ignoreTrailingZeros: true)
            .When(x => x.AutoCashOutAt.HasValue);

        RuleFor(x => x.Slot).InclusiveBetween(1, Bet.MaxSlot);
    }
}

public sealed class CancelRoundRequestValidator : AbstractValidator<CancelRoundRequest>
{
    public CancelRoundRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().Length(5, 500);
    }
}

public sealed class VerifyFairnessRequestValidator : AbstractValidator<VerifyFairnessRequest>
{
    public VerifyFairnessRequestValidator()
    {
        RuleFor(x => x.ServerSeed).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ServerSeedHash)
            .Matches("^[0-9a-fA-F]{64}$").WithMessage("Hash must be 64 hexadecimal characters.")
            .When(x => !string.IsNullOrEmpty(x.ServerSeedHash));
        RuleFor(x => x.CrashPoint).GreaterThanOrEqualTo(1).When(x => x.CrashPoint.HasValue);
    }
}
