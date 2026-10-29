namespace Veltrix.Domain.Common;

/// <summary>Values of Notification.Type. The Angular bell picks an icon (and an action) from it.</summary>
public static class NotificationTypes
{
    public const string DailyBonus = "DailyBonus";
    public const string BalanceAdjusted = "BalanceAdjusted";
    public const string RoleChanged = "RoleChanged";
    public const string AccountBlocked = "AccountBlocked";
    public const string AccountUnblocked = "AccountUnblocked";
    public const string PasswordChanged = "PasswordChanged";
    public const string SelfExcluded = "SelfExcluded";
    public const string AchievementUnlocked = "AchievementUnlocked";
    public const string LevelUp = "LevelUp";
}
