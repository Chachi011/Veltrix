using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Progress;

/// <summary>
/// XP and levels (spec 4.6). XP measures EXPERIENCE (how much you have played), not how much you won:
/// every settled bet counts, a cash-out a little more, and achievements add their own XP.
/// XP only grows, so a level is never lost.
/// </summary>
public static class Experience
{
    public const int PerSettledBet = 1;
    public const int ExtraForCashOut = 2;

    /// <summary>XP for one settled bet: 1 for a lost bet, 3 for a cashed-out one, 0 for anything else (refunds).</summary>
    public static int ForBet(Bet bet) => bet.Status switch
    {
        BetStatus.Lost => PerSettledBet,
        BetStatus.CashedOut => PerSettledBet + ExtraForCashOut,
        _ => 0
    };

    /// <summary>The highest level whose XP requirement is reached. <paramref name="levels"/> must contain level 1 (0 XP).</summary>
    public static Level LevelFor(int xp, IReadOnlyCollection<Level> levels) =>
        levels.Where(l => l.XpRequired <= xp).OrderByDescending(l => l.XpRequired).FirstOrDefault()
        ?? throw new DomainException("The level table has no level for 0 XP.");

    /// <summary>The next level after <paramref name="current"/>, or null at the top.</summary>
    public static Level? NextLevel(int current, IReadOnlyCollection<Level> levels) =>
        levels.Where(l => l.Number > current).OrderBy(l => l.Number).FirstOrDefault();
}

/// <summary>The codes of the achievements (the Achievements table is seeded with them).</summary>
public static class AchievementCodes
{
    public const string FirstBet = "FIRST_BET";
    public const string FirstCashOut = "FIRST_CASHOUT";
    public const string CashOut10x = "CASHOUT_10X";
    public const string Bets100 = "BETS_100";
    public const string BigWin1000 = "BIG_WIN_1000";
    public const string CashOuts50 = "CASHOUTS_50";
    public const string AutoCashOut = "AUTO_CASHOUT";
}

/// <summary>Where a player stands towards an achievement: e.g. 37 of 100 bets.</summary>
public readonly record struct AchievementProgress(decimal Current, decimal Target)
{
    public decimal Ratio => Target <= 0 ? 1 : Math.Min(1, Current / Target);
}

/// <summary>
/// When is an achievement earned? Pure rules, checked when a round is settled, so they are unit-tested without a database.
/// Most rules read the running totals (PlayerStats); "auto cash-out" looks at the bets of the round just settled.
/// </summary>
public static class AchievementRules
{
    public static bool IsEarned(string code, PlayerStats stats, IEnumerable<Bet> betsOfTheRound) => code switch
    {
        AchievementCodes.FirstBet => stats.TotalBets >= 1,
        AchievementCodes.FirstCashOut => stats.CashOutCount >= 1,
        AchievementCodes.CashOut10x => stats.BiggestMultiplier >= 10m,
        AchievementCodes.Bets100 => stats.TotalBets >= 100,
        AchievementCodes.BigWin1000 => stats.BiggestWin >= 1000m,
        AchievementCodes.CashOuts50 => stats.CashOutCount >= 50,
        // Paid by the auto cash-out itself: cashed out exactly at the target that was set
        AchievementCodes.AutoCashOut => betsOfTheRound.Any(b =>
            b.Status == BetStatus.CashedOut && b.AutoCashOutAt is { } target && b.CashOutMultiplier == target),
        _ => false
    };

    /// <summary>Progress for the achievements that have a counter (null for "one moment" achievements like auto cash-out).</summary>
    public static AchievementProgress? Progress(string code, PlayerStats? stats)
    {
        var s = stats ?? new PlayerStats();
        return code switch
        {
            AchievementCodes.FirstBet => new AchievementProgress(Math.Min(s.TotalBets, 1), 1),
            AchievementCodes.FirstCashOut => new AchievementProgress(Math.Min(s.CashOutCount, 1), 1),
            AchievementCodes.CashOut10x => new AchievementProgress(Math.Min(s.BiggestMultiplier, 10m), 10m),
            AchievementCodes.Bets100 => new AchievementProgress(Math.Min(s.TotalBets, 100), 100),
            AchievementCodes.BigWin1000 => new AchievementProgress(Math.Min(s.BiggestWin, 1000m), 1000m),
            AchievementCodes.CashOuts50 => new AchievementProgress(Math.Min(s.CashOutCount, 50), 50),
            _ => null
        };
    }
}
