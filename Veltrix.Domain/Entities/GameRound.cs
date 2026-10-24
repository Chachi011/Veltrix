using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities;

public class GameRound
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public long RoundNumber { get; set; }
    public RoundState State { get; set; } = RoundState.Waiting;

    /// <summary>Secret random seed. Revealed to players only after the round has crashed.</summary>
    public string ServerSeed { get; set; } = string.Empty;

    /// <summary>SHA-256(ServerSeed). Published BEFORE the round, so the result cannot be changed later.</summary>
    public string ServerSeedHash { get; set; } = string.Empty;

    /// <summary>Calculated from ServerSeed before the round starts.</summary>
    public decimal CrashPoint { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? BettingEndsAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CrashedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public Game? Game { get; set; }
    public ICollection<Bet> Bets { get; set; } = new List<Bet>();
}
