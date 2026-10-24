namespace Veltrix.Domain.Entities;

/// <summary>
/// An Admin's manual balance correction. The money movement itself is a
/// WalletTransaction (type AdminAdjustment); this row records WHO did it and WHY.
/// </summary>
public class BalanceAdjustment
{
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
}
