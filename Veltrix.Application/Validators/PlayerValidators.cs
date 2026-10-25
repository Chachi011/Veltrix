using FluentValidation;
using Veltrix.Application.DTOs.Leaderboard;
using Veltrix.Application.DTOs.Limits;
using Veltrix.Application.DTOs.Notifications;
using Veltrix.Application.DTOs.Stats;

namespace Veltrix.Application.Validators;

public sealed class LeaderboardQueryValidator : AbstractValidator<LeaderboardQuery>
{
    public LeaderboardQueryValidator()
    {
        RuleFor(x => x.Period).IsInEnum();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Limit).InclusiveBetween(1, LeaderboardQuery.MaxLimit);
    }
}

public sealed class MyStatsQueryValidator : AbstractValidator<MyStatsQuery>
{
    public MyStatsQueryValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(MyStatsQuery.MinDays, MyStatsQuery.MaxDays);
    }
}

public sealed class UpdateLimitsRequestValidator : AbstractValidator<UpdateLimitsRequest>
{
    public const decimal MaxDailyBetLimit = 1_000_000m;

    public UpdateLimitsRequestValidator()
    {
        RuleFor(x => x.DailyBetLimit)
            .GreaterThanOrEqualTo(1)
            .LessThanOrEqualTo(MaxDailyBetLimit)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("The limit can have at most 2 decimal places.")
            .When(x => x.DailyBetLimit.HasValue);

        RuleFor(x => x.SessionReminderMin)
            .Must(minutes => minutes == 0 || minutes is >= 15 and <= 600)
            .WithMessage("The session reminder must be 0 (off) or between 15 and 600 minutes.");
    }
}

public sealed class SelfExclusionRequestValidator : AbstractValidator<SelfExclusionRequest>
{
    public SelfExclusionRequestValidator()
    {
        RuleFor(x => x.Period).IsInEnum().WithMessage("Choose Hours24, Days7 or Days30.");
    }
}

public sealed class NotificationQueryValidator : PageQueryValidator<NotificationQuery>
{
}
