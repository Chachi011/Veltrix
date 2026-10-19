using System;
using System.Collections.Generic;
using System.Text;
using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities
{
    public class WalletTransaction
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public Guid PlayerId { get; set; }
        public Guid? BetId { get; set; }
        public Guid? GameRoundId { get; set; }
        public TransactionType Type { get; set; }
        public TransactionStatus Status { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Currency { get; set; } = "GEL";
        public string? ExternalTransactionId { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
