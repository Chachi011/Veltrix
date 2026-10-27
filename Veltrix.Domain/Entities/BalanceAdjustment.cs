using Veltrix.Domain.Common;
using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities;

/// <summary>
/// An Admin's manual balance correction. The money movement itself is a
/// WalletTransaction (type AdminAdjustment); this row records WHO did it and WHY.
/// </summary>
public class BalanceAdjustment
{
    public const int MinReasonLength = 5;
    public const int MaxReasonLength = 500;

    public int Id { get; set; }
    public int UserId { get; set; }
    public int AdminUserId { get; set; }

    /// <summary>Positive = add points, negative = remove points. Never zero.</summary>
    public decimal Amount { get; set; }

    /// <summary>Mandatory explanation.</summary>
    public string Reason { get; set; } = string.Empty;

    public int WalletTransactionId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public User? AdminUser { get; set; }
    public WalletTransaction? WalletTransaction { get; set; }

    /// <summary>
    /// Changes the wallet and returns the adjustment row linked to its ledger entry.
    /// Rules: a reason is required, the amount is not zero, the balance never goes negative,
    /// and an admin cannot adjust their own balance (nobody should be able to pay themselves).
    /// </summary>
    public static BalanceAdjustment Apply(
        Wallet wallet, int adminUserId, decimal amount, string reason, string idempotencyKey, DateTime now)
    {
        if (wallet.UserId == adminUserId)
            throw new DomainException("Admins cannot adjust their own balance.");

        if (amount == 0)
            throw new DomainException("The adjustment amount cannot be zero.");

        var trimmed = reason?.Trim() ?? string.Empty;
        if (trimmed.Length is < MinReasonLength or > MaxReasonLength)
            throw new DomainException($"A reason of {MinReasonLength} to {MaxReasonLength} characters is required.");

        if (amount < 0 && wallet.Balance < -amount)
            throw new DomainException(
                $"Cannot remove {-amount:0.00} VLX: the balance is only {wallet.Balance:0.00} VLX and can never go negative.");

        var transaction = amount > 0
            ? wallet.Credit(amount, TransactionType.AdminAdjustment, idempotencyKey, now, trimmed)
            : wallet.Debit(-amount, TransactionType.AdminAdjustment, idempotencyKey, now, trimmed);

        return new BalanceAdjustment
        {
            UserId = wallet.UserId,
            AdminUserId = adminUserId,
            Amount = amount,
            Reason = trimmed,
            WalletTransaction = transaction,
            CreatedAt = now
        };
    }
}
