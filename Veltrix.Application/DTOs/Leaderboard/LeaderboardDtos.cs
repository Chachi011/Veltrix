namespace Veltrix.Application.DTOs.Leaderboard;

/// <summary>day = last 24 hours, week = last 7 days, all = all time.</summary>
public enum LeaderboardPeriod
{
    Day = 1,
    Week = 2,
    All = 3
}

/// <summary>multiplier = best cash-out multiplier, profit = net virtual profit (payouts minus stakes).</summary>
public enum LeaderboardType
{
    Multiplier = 1,
    Profit = 2
}

/// <summary>?period=day|week|all&amp;type=multiplier|profit&amp;limit=20</summary>
public sealed class LeaderboardQuery
{
    public const int MaxLimit = 100;

    public LeaderboardPeriod Period { get; set; } = LeaderboardPeriod.Day;
    public LeaderboardType Type { get; set; } = LeaderboardType.Multiplier;
    public int Limit { get; set; } = 20;
}

public sealed class LeaderboardDto
{
    public LeaderboardPeriod Period { get; set; }
    public LeaderboardType Type { get; set; }

    /// <summary>Start of the period (null for all time).</summary>
    public DateTime? From { get; set; }

    public DateTime GeneratedAt { get; set; }
    public IReadOnlyList<LeaderboardEntryDto> Entries { get; set; } = Array.Empty<LeaderboardEntryDto>();
}

public sealed class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Level { get; set; }

    /// <summary>The multiplier (e.g. 12.34) or the profit in VLX, depending on the type.</summary>
    public decimal Value { get; set; }

    /// <summary>Settled bets of the player in the period.</summary>
    public int BetsCount { get; set; }
}
