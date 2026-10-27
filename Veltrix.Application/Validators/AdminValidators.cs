using FluentValidation;
using Veltrix.Application.DTOs.Admin;
using Veltrix.Application.DTOs.Stats;
using Veltrix.Domain.Entities;

namespace Veltrix.Application.Validators;

public sealed class AdminUserQueryValidator : PageQueryValidator<AdminUserQuery>
{
    public AdminUserQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Role).IsInEnum();
    }
}

public sealed class ChangeRoleRequestValidator : AbstractValidator<ChangeRoleRequest>
{
    public ChangeRoleRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum().WithMessage("Choose Player, Operator or Admin.");
    }
}

public sealed class BlockUserRequestValidator : AbstractValidator<BlockUserRequest>
{
    public BlockUserRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class BalanceAdjustmentRequestValidator : AbstractValidator<BalanceAdjustmentRequest>
{
    public const decimal MaxAdjustment = 1_000_000m;

    public BalanceAdjustmentRequestValidator()
    {
        RuleFor(x => x.Amount)
            .NotEqual(0).WithMessage("The amount cannot be zero.")
            .InclusiveBetween(-MaxAdjustment, MaxAdjustment)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("The amount can have at most 2 decimal places.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required.")
            .Must(reason => reason.Trim().Length is >= BalanceAdjustment.MinReasonLength and <= BalanceAdjustment.MaxReasonLength)
            .WithMessage($"The reason must be {BalanceAdjustment.MinReasonLength} to {BalanceAdjustment.MaxReasonLength} characters.");
    }
}

public sealed class AuditLogQueryValidator : PageQueryValidator<AuditLogQuery>
{
    public AuditLogQueryValidator()
    {
        RuleFor(x => x.Action).MaximumLength(100);
        RuleFor(x => x.EntityType).MaximumLength(100);
        RuleFor(x => x.EntityId).MaximumLength(100);
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From).When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("'To' must be after 'From'.");
    }
}

public sealed class UpdateSystemSettingsRequestValidator : AbstractValidator<UpdateSystemSettingsRequest>
{
    public UpdateSystemSettingsRequestValidator()
    {
        RuleFor(x => x.StartingBalance)
            .InclusiveBetween(0, 1_000_000)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true);

        RuleFor(x => x.DailyBonusAmount)
            .InclusiveBetween(1, 100_000)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true);
    }
}

public sealed class DailyStatsQueryValidator : AbstractValidator<DailyStatsQuery>
{
    public DailyStatsQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From).When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("'To' must be after 'From'.");

        RuleFor(x => x)
            .Must(x => x.To!.Value.DayNumber - x.From!.Value.DayNumber < DailyStatsQuery.MaxDays)
            .When(x => x.From.HasValue && x.To.HasValue)
            .OverridePropertyName("from")
            .WithMessage($"The range can be at most {DailyStatsQuery.MaxDays} days.");
    }
}
