namespace Veltrix.Domain.Entities;

/// <summary>Why and by whom a round was cancelled, and how much was refunded.</summary>
public class RoundCancellation
{
    public int Id { get; set; }
    public int RoundId { get; set; }

    /// <summary>Null = cancelled automatically by the system (e.g. engine error).</summary>
    public int? CancelledByUserId { get; set; }

    public string Reason { get; set; } = string.Empty;
    public int RefundedBetsCount { get; set; }
    public decimal RefundedTotal { get; set; }
    public DateTime CancelledAt { get; set; }

    public GameRound? Round { get; set; }
    public User? CancelledByUser { get; set; }
}
