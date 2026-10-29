namespace Veltrix.Domain.Entities;

/// <summary>Many-to-many join: which user unlocked which achievement. Key = (UserId, AchievementId).</summary>
public class UserAchievement
{
    public int UserId { get; set; }
    public int AchievementId { get; set; }
    public DateTime UnlockedAt { get; set; }

    public User? User { get; set; }
    public Achievement? Achievement { get; set; }
}
