using Veltrix.Domain.Common;
using Veltrix.Domain.Enums;
using Veltrix.Domain.Progress;

namespace Veltrix.Domain.Entities;

/// <summary>
/// Running totals per player, updated when a bet is settled. The leaderboard and public
/// profile read this single row instead of summing thousands of bets on every request.
/// Also holds the player's XP and level (spec 4.6). 1-to-1 with User.
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

    /// <summary>
    /// A bet of this player was settled (lost or cashed out): totals and records. No XP: betting more never gives
    /// progress (responsible play); XP comes only from achievements.
    /// </summary>
    public void AddSettledBet(Bet bet, DateTime now)
    {
        TotalBets++;
        TotalWagered += bet.Amount;

        if (bet.Status == BetStatus.CashedOut)
        {
            var payout = bet.Payout ?? 0;
            CashOutCount++;
            TotalPayout += payout;
            BiggestWin = Math.Max(BiggestWin, payout - bet.Amount);
            BiggestMultiplier = Math.Max(BiggestMultiplier, bet.CashOutMultiplier ?? 0);
        }

        UpdatedAt = now;
    }

    /// <summary>XP from an achievement.</summary>
    public void AddXp(int xp, DateTime now)
    {
        if (xp < 0)
            throw new DomainException("XP cannot be taken away.");
        Xp += xp;
        UpdatedAt = now;
    }

    /// <summary>Moves the player to the level their XP has reached. True when the level went up (never down).</summary>
    public bool UpdateLevel(IReadOnlyCollection<Level> levels)
    {
        var level = Experience.LevelFor(Xp, levels);
        if (level.Number <= LevelNumber)
            return false;

        LevelNumber = level.Number;
        return true;
    }
}
