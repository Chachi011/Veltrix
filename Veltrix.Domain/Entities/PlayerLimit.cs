using Veltrix.Domain.Common;
using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities;

/// <summary>
/// Responsible play tools of one player (1-to-1 with User): daily bet limit, session reminder, self-exclusion.
/// The rules live here, so they are unit-tested without a database.
/// </summary>
public class PlayerLimit
{
    public const int DefaultSessionReminderMin = 60;

    /// <summary>
    /// A STRICTER limit applies at once; a looser one (higher or removed) only after this delay.
    /// Otherwise the limit would be useless: a player on a losing streak could simply raise it and keep betting.
    /// </summary>
    public static readonly TimeSpan LimitIncreaseDelay = TimeSpan.FromHours(24);

    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Max total of bets per day (UTC). Null = no limit.</summary>
    public decimal? DailyBetLimit { get; set; }

    /// <summary>The waiting increase. Null together with a PendingLimitAppliesAt = "remove the limit".</summary>
    public decimal? PendingDailyBetLimit { get; set; }

    /// <summary>When the waiting increase takes effect. Null = nothing is waiting.</summary>
    public DateTime? PendingLimitAppliesAt { get; set; }

    /// <summary>Reminder interval during continuous play. 0 = off.</summary>
    public int SessionReminderMin { get; set; } = DefaultSessionReminderMin;

    public DateTime? SelfExcludedUntil { get; set; }

    public User? User { get; set; }

    public bool IsSelfExcluded(DateTime now) => SelfExcludedUntil > now;

    /// <summary>A change is still waiting (its time has not come yet).</summary>
    public bool HasPendingLimitChange(DateTime now) => PendingLimitAppliesAt > now;

    /// <summary>The limit in force at <paramref name="now"/>; a waiting increase counts once its time has come.</summary>
    public decimal? EffectiveDailyBetLimit(DateTime now) =>
        PendingLimitAppliesAt <= now ? PendingDailyBetLimit : DailyBetLimit;

    /// <summary>
    /// Checked before every bet. <paramref name="betToday"/> is the total already bet today (refunds excluded).
    /// </summary>
    public void EnsureCanBet(decimal amount, decimal betToday, DateTime now)
    {
        if (IsSelfExcluded(now))
            throw new SelfExcludedException(SelfExcludedUntil!.Value);

        if (EffectiveDailyBetLimit(now) is { } limit && betToday + amount > limit)
            throw new DailyBetLimitExceededException(betToday, limit);
    }

    /// <summary>
    /// Lowering the limit (or setting the first one) applies immediately.
    /// Raising or removing it waits <see cref="LimitIncreaseDelay"/>.
    /// </summary>
    public void ChangeDailyBetLimit(decimal? newLimit, DateTime now)
    {
        if (newLimit <= 0)
            throw new DomainException("The daily bet limit must be greater than zero.");

        ApplyDueLimitChange(now);

        var isStricter = newLimit.HasValue && (DailyBetLimit is null || newLimit <= DailyBetLimit);
        if (isStricter || newLimit == DailyBetLimit)
        {
            DailyBetLimit = newLimit;
            ClearPendingLimit();
            return;
        }

        // Asking again for the increase that is already waiting must not restart its 24 hours
        if (HasPendingLimitChange(now) && PendingDailyBetLimit == newLimit)
            return;

        PendingDailyBetLimit = newLimit;
        PendingLimitAppliesAt = now.Add(LimitIncreaseDelay);
    }

    /// <summary>Moves an increase whose time has come into DailyBetLimit.</summary>
    public void ApplyDueLimitChange(DateTime now)
    {
        if (PendingLimitAppliesAt > now || PendingLimitAppliesAt is null)
            return;

        DailyBetLimit = PendingDailyBetLimit;
        ClearPendingLimit();
    }

    public void ChangeSessionReminder(int minutes)
    {
        if (minutes is < 0 or > 600)
            throw new DomainException("The session reminder must be between 0 (off) and 600 minutes.");

        SessionReminderMin = minutes;
    }

    /// <summary>
    /// Pauses betting for 24 hours, 7 days or 30 days. It can be extended but never shortened or cancelled,
    /// because a pause the player can undo in a weak moment protects nobody.
    /// Returns the history row to store.
    /// </summary>
    public SelfExclusion StartSelfExclusion(SelfExclusionPeriod period, DateTime now)
    {
        var endsAt = now.Add(SelfExclusion.DurationOf(period));

        if (SelfExcludedUntil >= endsAt)
            throw new DomainException(
                $"You are already self-excluded until {SelfExcludedUntil:yyyy-MM-dd HH:mm} UTC. A self-exclusion cannot be shortened.");

        SelfExcludedUntil = endsAt;

        return new SelfExclusion { UserId = UserId, Period = period, StartsAt = now, EndsAt = endsAt };
    }

    private void ClearPendingLimit()
    {
        PendingDailyBetLimit = null;
        PendingLimitAppliesAt = null;
    }
}
