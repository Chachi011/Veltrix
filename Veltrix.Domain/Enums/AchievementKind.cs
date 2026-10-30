namespace Veltrix.Domain.Enums;

/// <summary>
/// What an achievement (a task) asks the player to do. Responsible-play rules depend on it:
/// a Play task is hidden from a player who cannot or should not bet right now; Learn and Safety tasks never are.
/// </summary>
public enum AchievementKind
{
    /// <summary>Needs a bet (try the game, try cash-out, try auto cash-out). Always ONE action, never a count.</summary>
    Play = 0,

    /// <summary>Understand the game: read the rules, verify a round.</summary>
    Learn = 1,

    /// <summary>Use a responsible-play tool: set a daily limit.</summary>
    Safety = 2
}
