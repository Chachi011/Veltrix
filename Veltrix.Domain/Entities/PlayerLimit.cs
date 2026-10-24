namespace Veltrix.Domain.Entities;

public class PlayerLimit
{
    public const int DefaultSessionReminderMin = 60;

    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Max total of bets per day. Null = no limit.</summary>
    public decimal? DailyBetLimit { get; set; }

    /// <summary>Reminder interval during continuous play. 0 = off.</summary>
    public int SessionReminderMin { get; set; } = DefaultSessionReminderMin;

    public DateTime? SelfExcludedUntil { get; set; }

    public User? User { get; set; }
}
