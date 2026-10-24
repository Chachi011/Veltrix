using Veltrix.Application.Common.Models;
using Veltrix.Domain.Enums;

namespace Veltrix.Application.DTOs.Rounds;

public sealed class RoundDto
{
    public int Id { get; set; }
    public string GameSlug { get; set; } = string.Empty;
    public long RoundNumber { get; set; }
    public RoundState State { get; set; }

    /// <summary>Always visible: the commitment published before the round.</summary>
    public string ServerSeedHash { get; set; } = string.Empty;

    /// <summary>Null until the round is over (Crashed / Finished / Cancelled).</summary>
    public string? ServerSeed { get; set; }

    /// <summary>Null until the round is over. Showing it earlier would let players cheat.</summary>
    public decimal? CrashPoint { get; set; }

    /// <summary>Only while Running: the server's multiplier at the moment of the request.</summary>
    public decimal? CurrentMultiplier { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? BettingEndsAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CrashedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}

/// <summary>One line of the live bets list. No private data (no auto cash-out target, no user id).</summary>
public sealed class RoundBetDto
{
    public int BetId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public decimal Amount { get; set; }
    public int Slot { get; set; }
    public BetStatus Status { get; set; }
    public decimal? CashOutMultiplier { get; set; }
    public decimal? Payout { get; set; }
    public DateTime PlacedAt { get; set; }
}

public sealed class CancelRoundRequest
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class RoundCancellationDto
{
    public int RoundId { get; set; }
    public int RefundedBetsCount { get; set; }
    public decimal RefundedTotal { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CancelledAt { get; set; }
}

public sealed class RoundHistoryQuery : PageQuery
{
}
