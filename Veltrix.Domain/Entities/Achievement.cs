namespace Veltrix.Domain.Entities;

public class Achievement
{
    public int Id { get; set; }

    /// <summary>Stable code used by the code that unlocks it, e.g. "FIRST_CASHOUT".</summary>
    public string Code { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int XpReward { get; set; }

    public ICollection<UserAchievement> UnlockedBy { get; set; } = new List<UserAchievement>();
}
