using Veltrix.Application.Common.Models;
using Veltrix.Domain.Enums;

namespace Veltrix.Application.DTOs.Wallet;

public sealed class WalletDto
{
    public decimal Balance { get; set; }
    public decimal DailyBonusAmount { get; set; }
    public bool CanClaimDailyBonus { get; set; }
    public DateTime? LastDailyBonusAt { get; set; }
    public DateTime? NextDailyBonusAt { get; set; }
}

public sealed class WalletTransactionDto
{
    public int Id { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public int? BetId { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class TransactionQuery : PageQuery
{
    public TransactionType? Type { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
