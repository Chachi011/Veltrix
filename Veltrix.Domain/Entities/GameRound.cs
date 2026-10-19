using System;
using System.Collections.Generic;
using System.Text;
using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities
{
    public class GameRound
    {
        public Guid Id { get; set; }
        public Guid GameId { get; set; }
        public RoundStatus Status { get; set; }
        public decimal CrashPoint { get; set; }
        public decimal CurrentMultiplier { get; set; } = 1.00m;
        public string ServerSeedHash { get; set; } = string.Empty;
        public string? ServerSeed { get; set; }
        public string? ClientSeed { get; set; }
        public long Nonce { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? BettingOpenedAt { get; set; }
        public DateTime? BettingClosedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CrashedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
    }
}
