namespace Veltrix.Domain.Common;

/// <summary>
/// Keys of the SystemSettings table (editable by Admin at runtime).
/// </summary>
public static class SettingKeys
{
    public const string StartingBalance = "Wallet.StartingBalance";
    public const string DailyBonusAmount = "Wallet.DailyBonusAmount";
}

/// <summary>Values used when a setting row is missing (the migration seeds both rows).</summary>
public static class SettingDefaults
{
    public const decimal StartingBalance = 1000m;
    public const decimal DailyBonusAmount = 100m;
}
