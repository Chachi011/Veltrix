using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities;

public class Bet
{
    public int Id { get; set; }
    public int RoundId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }

    /// <summary>1 or 2 (slot 2 is the optional "two bets per round" feature).</summary>
    public int Slot { get; set; } = 1;

    public decimal? AutoCashOutAt { get; set; }
    public decimal? CashOutMultiplier { get; set; }
    public decimal? Payout { get; set; }
    public BetStatus Status { get; set; } = BetStatus.Active;
    public DateTime PlacedAt { get; set; }
    public DateTime? CashedOutAt { get; set; }

    public GameRound? Round { get; set; }
    public User? User { get; set; }
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
}
