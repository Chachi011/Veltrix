using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities;

public class WalletTransaction
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public TransactionType Type { get; set; }

    /// <summary>Signed amount: negative for Bet, positive for Win/Refund/Bonus. BalanceAfter = BalanceBefore + Amount.</summary>
    public decimal Amount { get; set; }

    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public int? BetId { get; set; }

    /// <summary>Unique key of the operation, so the same operation can never be applied twice.</summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }

    public Wallet? Wallet { get; set; }
    public Bet? Bet { get; set; }
}
