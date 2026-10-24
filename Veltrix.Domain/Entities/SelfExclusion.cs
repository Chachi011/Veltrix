using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities;

/// <summary>
/// History of self-exclusions. PlayerLimit.SelfExcludedUntil holds the CURRENT one;
/// this table keeps every one the player has ever started.
/// </summary>
public class SelfExclusion
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public SelfExclusionPeriod Period { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public User? User { get; set; }

    public static TimeSpan DurationOf(SelfExclusionPeriod period) => period switch
    {
        SelfExclusionPeriod.Hours24 => TimeSpan.FromHours(24),
        SelfExclusionPeriod.Days7 => TimeSpan.FromDays(7),
        SelfExclusionPeriod.Days30 => TimeSpan.FromDays(30),
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, null)
    };
}
