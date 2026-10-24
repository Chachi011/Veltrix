namespace Veltrix.Domain.Entities;

/// <summary>
/// Optional feature: "bet X every round, cash out at Y, for N rounds".
/// The server places the bets, so auto-bet keeps working even if the browser tab is closed.
/// </summary>
public class AutoBetConfig
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int GameId { get; set; }
    public decimal Amount { get; set; }
    public decimal AutoCashOutAt { get; set; }
    public int TotalRounds { get; set; }
    public int RoundsPlayed { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? StoppedAt { get; set; }

    public User? User { get; set; }
    public Game? Game { get; set; }
}
