using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Entities;

public class Game
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GameType Type { get; set; } = GameType.Crash;
    public GameStatus Status { get; set; } = GameStatus.Active;
    public decimal MinBet { get; set; }
    public decimal MaxBet { get; set; }
    public int BettingDurationSec { get; set; }
    public int PauseBetweenRoundsSec { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<GameRound> Rounds { get; set; } = new List<GameRound>();
}
