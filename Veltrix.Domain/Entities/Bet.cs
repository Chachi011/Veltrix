using System;
using System.Collections.Generic;
using System.Text;
using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities
{
    public class Bet
    {
        public Guid Id { get; set; }
        public Guid PlayerId { get; set; }
        public Guid GameRoundId { get; set; }
        public decimal Amount { get; set; }
        public BetStatus Status { get; set; }
        public decimal? CashoutMultiplier { get; set; }
        public decimal? WinAmount { get; set; }
        public DateTime PlaceAt { get; set; } = DateTime.UtcNow;
        public DateTime? CashoutAt { get; set; }
        public DateTime? SettledAt { get; set; }
    }
}
