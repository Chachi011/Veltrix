namespace Veltrix.Domain.Enums;

/// <summary>
/// Waiting → BettingOpen → BettingClosed → Running → Crashed → Finished.
/// Any non-final state can go to Cancelled.
/// </summary>
public enum RoundState
{
    Waiting = 0,
    BettingOpen = 1,
    BettingClosed = 2,
    Running = 3,
    Crashed = 4,
    Finished = 5,
    Cancelled = 6
}
