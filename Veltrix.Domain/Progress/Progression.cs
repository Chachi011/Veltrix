using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;

namespace Veltrix.Domain.Progress;

/// <summary>
/// XP and levels (spec 4.6). XP comes ONLY from achievements (tasks): learning the game, using the safety tools and
/// trying each part of the game once. Betting more, staking more, winning more or playing longer never gives XP,
/// so a level shows what the player has learned, not how much they gambled. XP only grows, so a level is never lost.
/// </summary>
public static class Experience
{
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
    // Play: one action each, checked when a round is settled
    public const string FirstBet = "FIRST_BET";
    public const string FirstCashOut = "FIRST_CASHOUT";
    public const string AutoCashOut = "AUTO_CASHOUT";

    // Learn and Safety: completed by an action outside a round
    public const string VerifyRound = "VERIFY_ROUND";
    public const string ReadRules = "READ_RULES";
    public const string SetDailyLimit = "SET_DAILY_LIMIT";

    /// <summary>
    /// Retired: they rewarded betting volume, stake size or chasing high multipliers (100 bets, 50 cash-outs,
    /// a 1000 VLX win, a 10x cash-out). They stay in the table as inactive rows only for the history.
    /// </summary>
    public static readonly IReadOnlyList<string> Retired = new[] { "CASHOUT_10X", "BETS_100", "BIG_WIN_1000", "CASHOUTS_50" };

    /// <summary>Checked by the round settlement.</summary>
    public static readonly IReadOnlyList<string> CompletedBySettlement = new[] { FirstBet, FirstCashOut, AutoCashOut };

    /// <summary>Completed by their own action: verifying a round, reading the rules, saving a daily limit.</summary>
    public static readonly IReadOnlyList<string> CompletedByAction = new[] { VerifyRound, ReadRules, SetDailyLimit };
}

/// <summary>Where a player stands towards an achievement: 0 / 1 or 1 / 1 (every task is a single step).</summary>
public readonly record struct AchievementProgress(decimal Current, decimal Target)
{
    public decimal Ratio => Target <= 0 ? 1 : Math.Min(1, Current / Target);
}

/// <summary>
/// When is a Play achievement earned? Pure rules, checked when a round is settled, so they are unit-tested without a database.
/// Responsible-play rule: a task may never reward betting volume, the amount staked, the number of bets, the size of a win
/// or the time played. So every rule is "did it happen at least once", and none looks at an amount
/// (ResponsibleAchievementTests checks this for every rule).
/// </summary>
public static class AchievementRules
{
    public static bool IsEarnedAtSettlement(string code, PlayerStats stats, IEnumerable<Bet> betsOfTheRound) => code switch
    {
        // "Try the game": one settled bet of any size
        AchievementCodes.FirstBet => stats.TotalBets >= 1,
        // "Try cash-out": the core mechanic, once
        AchievementCodes.FirstCashOut => stats.CashOutCount >= 1,
        // "Plan ahead": a settled bet with an auto cash-out target, won or lost (it rewards planning, not winning)
        AchievementCodes.AutoCashOut => betsOfTheRound.Any(b =>
            b.AutoCashOutAt is not null && b.Status is BetStatus.CashedOut or BetStatus.Lost),
        _ => false
    };

    /// <summary>Progress towards the achievement: always a single step (0 / 1 or 1 / 1); null when it cannot be read from the totals.</summary>
    public static AchievementProgress? Progress(string code, PlayerStats? stats)
    {
        var s = stats ?? new PlayerStats();
        return code switch
        {
            AchievementCodes.FirstBet => new AchievementProgress(Math.Min(s.TotalBets, 1), 1),
            AchievementCodes.FirstCashOut => new AchievementProgress(Math.Min(s.CashOutCount, 1), 1),
            _ => null
        };
    }

    /// <summary>Content pages whose reading completes a task: the end of the Rules page ("no winning strategy").</summary>
    public static string? CompletedByReading(string pageSlug) =>
        string.Equals(pageSlug, "no-winning-strategy", StringComparison.OrdinalIgnoreCase) ? AchievementCodes.ReadRules : null;

    /// <summary>Shown first: safety, then learning, then play (the platform prefers the first two).</summary>
    public static int DisplayOrder(AchievementKind kind) => kind switch
    {
        AchievementKind.Safety => 0,
        AchievementKind.Learn => 1,
        _ => 2
    };
}

/// <summary>Who sees which achievement (responsible play).</summary>
public static class AchievementVisibility
{
    /// <summary>
    /// A player who cannot bet right now (self-excluded, or the daily limit is used up) sees no task that asks them to play.
    /// Learn and Safety tasks stay visible, and so does a Play task they already completed (it no longer asks anything).
    /// </summary>
    public static bool IsShown(AchievementKind kind, bool unlocked, bool canPlayNow) =>
        unlocked || canPlayNow || kind != AchievementKind.Play;
}
