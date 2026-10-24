namespace Veltrix.Domain.Entities;

/// <summary>
/// Running totals per player, updated when a bet is settled. The leaderboard and public
/// profile read this single row instead of summing thousands of bets on every request.
/// 1-to-1 with User.
/// </summary>
public class PlayerStats
{
    public int UserId { get; set; }
    public int TotalBets { get; set; }
    public int CashOutCount { get; set; }
    public decimal TotalWagered { get; set; }
    public decimal TotalPayout { get; set; }
    public decimal BiggestWin { get; set; }
    public decimal BiggestMultiplier { get; set; }
    public int Xp { get; set; }
    public int LevelNumber { get; set; } = 1;
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
    public Level? Level { get; set; }
}
