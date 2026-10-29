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
}

/// <summary>An achievement from the logged-in player's point of view: unlocked (when) or the progress so far.</summary>
public sealed class MyAchievementDto : AchievementDto
{
    public DateTime? UnlockedAt { get; set; }

    /// <summary>e.g. 37 of 100 bets; null for "one moment" achievements (auto cash-out).</summary>
    public decimal? Progress { get; set; }

    public decimal? Target { get; set; }
}

/// <summary>An unlocked achievement shown on a public profile.</summary>
public sealed class BadgeDto
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public DateTime UnlockedAt { get; set; }
}
