using Veltrix.Domain.Enums;

namespace Veltrix.Application.DTOs.Games;

public sealed class GameDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public GameType Type { get; set; }
    public GameStatus Status { get; set; }
    public decimal MinBet { get; set; }
    public decimal MaxBet { get; set; }
    public int BettingDurationSec { get; set; }
    public int PauseBetweenRoundsSec { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Used for both create (POST) and edit (PUT).</summary>
public sealed class SaveGameRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MinBet { get; set; }
    public decimal MaxBet { get; set; }
    public int BettingDurationSec { get; set; } = 10;
    public int PauseBetweenRoundsSec { get; set; } = 5;
}

public sealed class UpdateGameStatusRequest
{
    public GameStatus Status { get; set; }
}

public sealed class GameQuery
{
    public string? Search { get; set; }
    public GameType? Type { get; set; }
    public GameStatus? Status { get; set; }
}
