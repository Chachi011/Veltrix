using Veltrix.Domain.Common;
using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities;

public class Wallet
{
    public static readonly TimeSpan DailyBonusInterval = TimeSpan.FromHours(24);

    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>Virtual VLX points. A DB CHECK constraint guarantees it is never negative.</summary>
    public decimal Balance { get; set; }

    public DateTime? LastDailyBonusAt { get; set; }

    /// <summary>
    /// Optimistic concurrency token. SQL Server changes it on every update, so if two requests
    /// modify the same wallet at the same time, the second one fails instead of overwriting the first.
    /// </summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public User? User { get; set; }
    public ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();

    /// <summary>
    /// Takes points out (a bet). The ONLY way to decrease the balance, and it refuses to go below zero.
    /// Every change creates its ledger row with the balance before and after.
    /// </summary>
    public WalletTransaction Debit(decimal amount, TransactionType type, string idempotencyKey, DateTime now, string? reason = null, Bet? bet = null)
    {
        EnsurePositive(amount);

        if (Balance < amount)
            throw new InsufficientBalanceException(Balance, amount);

        return Apply(-amount, type, idempotencyKey, now, reason, bet);
    }

    /// <summary>Adds points (win, refund, bonus).</summary>
    public WalletTransaction Credit(decimal amount, TransactionType type, string idempotencyKey, DateTime now, string? reason = null, Bet? bet = null)
    {
        EnsurePositive(amount);
        return Apply(amount, type, idempotencyKey, now, reason, bet);
    }

    public DateTime? NextDailyBonusAt => LastDailyBonusAt?.Add(DailyBonusInterval);

    public bool CanClaimDailyBonus(DateTime now) => NextDailyBonusAt is null || NextDailyBonusAt <= now;

    public WalletTransaction ClaimDailyBonus(decimal amount, DateTime now)
    {
        if (!CanClaimDailyBonus(now))
            throw new DomainException($"Daily bonus is available again at {NextDailyBonusAt:yyyy-MM-dd HH:mm} UTC.");

        // Key built from the PREVIOUS claim time: two parallel claims produce the same key,
        // so the unique index lets only one of them in.
        var key = $"daily-bonus:{Id}:{LastDailyBonusAt?.Ticks ?? 0}";

        LastDailyBonusAt = now;
        return Credit(amount, TransactionType.Bonus, key, now, "Daily bonus");
    }

    private WalletTransaction Apply(decimal signedAmount, TransactionType type, string idempotencyKey, DateTime now, string? reason, Bet? bet)
    {
        var transaction = new WalletTransaction
        {
            WalletId = Id,
            Type = type,
            Amount = signedAmount,
            BalanceBefore = Balance,
            BalanceAfter = Balance + signedAmount,
            IdempotencyKey = idempotencyKey,
            Reason = reason,
            CreatedAt = now,
            Bet = bet
        };

        Balance = transaction.BalanceAfter;
        Transactions.Add(transaction);
        return transaction;
    }

    private static void EnsurePositive(decimal amount)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero.");
    }
}
