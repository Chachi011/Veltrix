using Veltrix.Domain.Enums;

namespace Veltrix.Application.DTOs.Limits;

/// <summary>The player's responsible play settings (GET /api/limits).</summary>
public sealed class PlayerLimitsDto
{
    /// <summary>The limit in force now. Null = no limit.</summary>
    public decimal? DailyBetLimit { get; set; }

    /// <summary>True when a higher limit (or its removal) is waiting for the 24-hour delay.</summary>
    public bool HasPendingLimitChange { get; set; }

    /// <summary>The waiting value. Null together with HasPendingLimitChange = the limit will be removed.</summary>
    public decimal? PendingDailyBetLimit { get; set; }

    public DateTime? PendingLimitAppliesAt { get; set; }

    /// <summary>Total of today's bets (UTC day, refunds excluded).</summary>
    public decimal BetToday { get; set; }

    /// <summary>What can still be bet today. Null when there is no limit.</summary>
    public decimal? RemainingToday { get; set; }

    /// <summary>Next 00:00 UTC, when BetToday starts again from zero.</summary>
    public DateTime DayResetsAt { get; set; }

    /// <summary>Minutes between "you have been playing for a while" reminders. 0 = off.</summary>
    public int SessionReminderMin { get; set; }

    public bool IsSelfExcluded { get; set; }
    public DateTime? SelfExcludedUntil { get; set; }

    /// <summary>How long a higher limit waits, so the UI can explain it.</summary>
    public int LimitIncreaseDelayHours { get; set; }
}

public sealed class UpdateLimitsRequest
{
    /// <summary>Null = no limit. Lowering applies at once; raising or removing applies after 24 hours.</summary>
    public decimal? DailyBetLimit { get; set; }

    /// <summary>0 = off, otherwise 15-600 minutes.</summary>
    public int SessionReminderMin { get; set; } = 60;
}

public sealed class SelfExclusionRequest
{
    /// <summary>Hours24, Days7 or Days30.</summary>
    public SelfExclusionPeriod Period { get; set; }
}
