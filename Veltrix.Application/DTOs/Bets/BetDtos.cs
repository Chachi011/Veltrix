using Veltrix.Application.Common.Models;
using Veltrix.Domain.Enums;

namespace Veltrix.Application.DTOs.Bets;

/// <summary>
/// The client only says WHAT it wants to bet. Which round, whether betting is open, the
/// multiplier and the payout are all decided by the server.
/// </summary>
public sealed class PlaceBetRequest
{
    public string GameSlug { get; set; } = "crash";
    public decimal Amount { get; set; }

    /// <summary>Optional: the server cashes out automatically at this multiplier.</summary>
    public decimal? AutoCashOutAt { get; set; }

    /// <summary>1 or 2.</summary>
    public int Slot { get; set; } = 1;
}

public sealed class BetDto
{
    public int Id { get; set; }
    public int RoundId { get; set; }
    public long RoundNumber { get; set; }
    public string GameSlug { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Slot { get; set; }
    public decimal? AutoCashOutAt { get; set; }
    public BetStatus Status { get; set; }
    public decimal? CashOutMultiplier { get; set; }
    public decimal? Payout { get; set; }

    /// <summary>Only after the round is over.</summary>
    public decimal? CrashPoint { get; set; }

    public DateTime PlacedAt { get; set; }
    public DateTime? CashedOutAt { get; set; }
}

/// <summary>Response of place-bet and cash-out: the bet plus the new balance.</summary>
public sealed class BetResultDto
{
    public BetDto Bet { get; set; } = new();
    public decimal Balance { get; set; }
}

public sealed class MyBetsQuery : PageQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public BetStatus? Status { get; set; }
}
