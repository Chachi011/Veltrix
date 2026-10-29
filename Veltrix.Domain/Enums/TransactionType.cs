namespace Veltrix.Domain.Enums;

/// <summary>
/// There is intentionally no Deposit/Withdrawal: VLX points can never be bought or cashed out.
/// </summary>
public enum TransactionType
{
    Bet = 1,
    Win = 2,
    Refund = 3,
    Bonus = 4,
    AdminAdjustment = 5
}
