using Veltrix.Domain.Enums;

namespace Veltrix.Application.DTOs.Achievements;

/// <summary>One achievement of the catalog (spec 4.6), with how many players have unlocked it.</summary>
public class AchievementDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Icon name chosen by the client ("rocket", "bolt", "trophy", ...).</summary>
    public string Icon { get; set; } = string.Empty;

    public int XpReward { get; set; }
    public int VlxReward { get; set; }
    public int UnlockedBy { get; set; }

    /// <summary>Play, Learn or Safety.</summary>
    public AchievementKind Kind { get; set; }
}

/// <summary>An achievement from the logged-in player's point of view: unlocked (when) or the progress so far.</summary>
public sealed class MyAchievementDto : AchievementDto
{
    public DateTime? UnlockedAt { get; set; }

    /// <summary>0 or 1 of 1 for the ones the totals can show (first bet, first cash-out); null for the others.</summary>
    public decimal? Progress { get; set; }

    public decimal? Target { get; set; }
}

/// <summary>The logged-in player's achievements.</summary>
public sealed class MyAchievementsDto
{
    public IReadOnlyList<MyAchievementDto> Items { get; set; } = Array.Empty<MyAchievementDto>();

    /// <summary>True when tasks that ask to play were left out: the player is self-excluded or used up today's limit.</summary>
    public bool PlayTasksHidden { get; set; }
}

/// <summary>An unlocked achievement shown on a public profile.</summary>
public sealed class BadgeDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public DateTime UnlockedAt { get; set; }
}
