namespace Veltrix.Domain.Entities;

/// <summary>
/// One pre-calculated row per day for the Admin dashboard charts.
/// Calculating it once per day is much cheaper than scanning all bets on every page load.
/// </summary>
public class DailyStatistic
{
    public DateOnly Date { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsers { get; set; }
    public int RoundsPlayed { get; set; }
    public int BetsCount { get; set; }
    public decimal BetsVolume { get; set; }
    public decimal PayoutVolume { get; set; }
    public decimal AverageCrashPoint { get; set; }
    public DateTime UpdatedAt { get; set; }
}
