using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities;

/// <summary>
/// One row per state transition of a round (e.g. BettingOpen → BettingClosed).
/// Gives the Operator a full timeline of each round and helps debug the engine.
/// </summary>
public class GameRoundEvent
{
    public int Id { get; set; }
    public int RoundId { get; set; }

    /// <summary>Null for the very first event (round created).</summary>
    public RoundState? FromState { get; set; }

    public RoundState ToState { get; set; }
    public string? Note { get; set; }
    public DateTime OccurredAt { get; set; }

    public GameRound? Round { get; set; }
}
