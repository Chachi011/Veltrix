namespace Veltrix.Application.DTOs.Stats;

/// <summary>?days=30: how many days the daily chart covers.</summary>
public sealed class MyStatsQuery
{
    public const int MinDays = 7;
    public const int MaxDays = 90;

    public int Days { get; set; } = 30;
}

/// <summary>Personal statistics (GET /api/stats/me). Only settled bets count (cashed out or lost).</summary>
public sealed class MyStatsDto
{
    public int TotalBets { get; set; }
    public int CashOutCount { get; set; }
    public int LostCount { get; set; }

    /// <summary>Share of bets that were cashed out, in percent (0-100).</summary>
    public decimal WinRate { get; set; }

    public decimal TotalWagered { get; set; }
    public decimal TotalPayout { get; set; }
    public decimal NetProfit { get; set; }
    public decimal AverageBet { get; set; }

    /// <summary>Average multiplier of the cashed-out bets.</summary>
    public decimal AverageCashOut { get; set; }

    public decimal BiggestMultiplier { get; set; }
    public decimal BiggestWin { get; set; }

    /// <summary>One point per day (days without bets included, with zeros), oldest first.</summary>
    public IReadOnlyList<DailyPointDto> Daily { get; set; } = Array.Empty<DailyPointDto>();

    /// <summary>Where the player cashes out: count of cash-outs per multiplier range.</summary>
    public IReadOnlyList<MultiplierBucketDto> CashOutDistribution { get; set; } = Array.Empty<MultiplierBucketDto>();
}

public sealed class DailyPointDto
{
    public DateOnly Date { get; set; }
    public int Bets { get; set; }
    public int CashOuts { get; set; }
    public decimal Wagered { get; set; }
    public decimal Payout { get; set; }
    public decimal Profit { get; set; }
}

public sealed class MultiplierBucketDto
{
    public string Label { get; set; } = string.Empty;
    public decimal From { get; set; }

    /// <summary>Exclusive upper bound; null for the last, open-ended range.</summary>
    public decimal? To { get; set; }

    public int Count { get; set; }

    /// <summary>Observed share in percent.</summary>
    public decimal Percent { get; set; }

    /// <summary>Share predicted by the provably fair formula, in percent (crash points only).</summary>
    public decimal? ExpectedPercent { get; set; }
}

/// <summary>Admin dashboard header (GET /api/admin/stats/overview). "Today" = since 00:00 UTC.</summary>
public sealed class AdminOverviewDto
{
    public int TotalUsers { get; set; }
    public int PlayersCount { get; set; }
    public int BlockedUsers { get; set; }
    public int NewUsersToday { get; set; }

    /// <summary>Open real-time connections (browser tabs) right now, guests included.</summary>
    public int OnlineNow { get; set; }

    public int ActivePlayersToday { get; set; }
    public int RoundsToday { get; set; }
    public int BetsToday { get; set; }
    public decimal BetsVolumeToday { get; set; }
    public decimal PayoutToday { get; set; }

    /// <summary>Stakes minus payouts today: what the house kept (virtual points).</summary>
    public decimal HouseResultToday { get; set; }

    /// <summary>Sum of all wallet balances.</summary>
    public decimal VlxInCirculation { get; set; }

    /// <summary>How many recent finished rounds the distribution below is based on.</summary>
    public int RoundsSampled { get; set; }

    public decimal AverageCrashPoint { get; set; }

    /// <summary>Observed vs expected share of crash points per range.</summary>
    public IReadOnlyList<MultiplierBucketDto> CrashPointDistribution { get; set; } = Array.Empty<MultiplierBucketDto>();
}

/// <summary>?from=2026-10-01&amp;to=2026-10-25 (UTC dates). Default: the last 30 days.</summary>
public sealed class DailyStatsQuery
{
    public const int MaxDays = 366;
    public const int DefaultDays = 30;

    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

/// <summary>One day of the admin charts. Past days come from the DailyStatistics table; today is calculated live.</summary>
public sealed class DailyStatisticDto
{
    public DateOnly Date { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsers { get; set; }
    public int RoundsPlayed { get; set; }
    public int BetsCount { get; set; }
    public decimal BetsVolume { get; set; }
    public decimal PayoutVolume { get; set; }
    public decimal AverageCrashPoint { get; set; }
}
