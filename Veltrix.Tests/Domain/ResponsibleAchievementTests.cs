using Microsoft.EntityFrameworkCore;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Domain.Progress;
using Veltrix.Tests.Services;
using static Veltrix.Tests.Domain.ProgressionTests;

namespace Veltrix.Tests.Domain;

/// <summary>
/// The responsible-play rules for achievements (tasks):
/// 1. No task may reward betting volume, the amount staked, the number of bets or the time played.
/// 2. Variety, learning and safety tasks are preferred (try the game, verify a round, set a daily limit, read the rules).
/// 3. A self-excluded player, or one whose daily limit is used up, sees no task that asks them to play.
/// </summary>
public class ResponsibleAchievementTests
{
    private static readonly DateTime Now = TestData.Now;

    // ---------- Rule 1: never volume, stake, count or time ----------

    /// <summary>Someone who played once with the smallest stake, and someone who played 100,000 times with huge stakes.</summary>
    private static readonly PlayerStats PlayedOnceSmall = new()
    {
        TotalBets = 1, CashOutCount = 1, TotalWagered = 1m, TotalPayout = 1.01m, BiggestWin = 0.01m, BiggestMultiplier = 1.01m
    };

    private static readonly PlayerStats PlayedALotBig = new()
    {
        TotalBets = 100_000, CashOutCount = 60_000, TotalWagered = 900_000_000m, TotalPayout = 950_000_000m,
        BiggestWin = 5_000_000m, BiggestMultiplier = 1_000m
    };

    [Fact]
    public void Playing_more_or_staking_more_never_earns_more_than_playing_once()
    {
        foreach (var code in AchievementCodes.CompletedBySettlement)
        {
            foreach (var round in new[]
                     {
                         Array.Empty<Bet>(),
                         new[] { Settled(BetStatus.Lost, amount: 1m, auto: 2m) },
                         new[] { Settled(BetStatus.CashedOut, amount: 1m, multiplier: 1.01m, auto: 1.01m) }
                     })
            {
                Assert.Equal(
                    AchievementRules.IsEarnedAtSettlement(code, PlayedOnceSmall, round),
                    AchievementRules.IsEarnedAtSettlement(code, PlayedALotBig, round));
            }
        }
    }

    [Fact]
    public void The_size_of_the_stake_or_the_win_in_the_round_changes_nothing()
    {
        var stats = new PlayerStats { TotalBets = 1 };

        foreach (var code in AchievementCodes.CompletedBySettlement)
        {
            var small = new[] { Settled(BetStatus.CashedOut, amount: 1m, multiplier: 1.01m, auto: 1.01m) };
            var huge = new[] { Settled(BetStatus.CashedOut, amount: 10_000m, multiplier: 100m, auto: 100m) };

            Assert.Equal(AchievementRules.IsEarnedAtSettlement(code, stats, small), AchievementRules.IsEarnedAtSettlement(code, stats, huge));
        }
    }

    [Fact]
    public void Every_task_is_a_single_step_not_a_counter()
    {
        foreach (var code in AchievementCodes.CompletedBySettlement.Concat(AchievementCodes.CompletedByAction))
        {
            var target = AchievementRules.Progress(code, PlayedALotBig)?.Target ?? 1;
            Assert.True(target <= 1, $"{code} counts up to {target}");
        }
    }

    // ---------- Rule 2: the seeded catalog prefers learning and safety ----------

    private static List<Achievement> SeededAchievements(out List<Level> levels)
    {
        using var ctx = new ServiceTestContext(); // EnsureCreated applies the HasData seed
        levels = ctx.Db.Levels.AsNoTracking().ToList();
        return ctx.Db.Achievements.AsNoTracking().ToList();
    }

    [Fact]
    public void The_volume_achievements_are_retired_and_every_active_one_has_a_trigger()
    {
        var all = SeededAchievements(out _);
        var active = all.Where(a => a.IsActive).Select(a => a.Code).ToList();

        Assert.All(AchievementCodes.Retired, code => Assert.Contains(all, a => a.Code == code && !a.IsActive));
        Assert.Equal(
            AchievementCodes.CompletedBySettlement.Concat(AchievementCodes.CompletedByAction).Order(),
            active.Order());
    }

    [Fact]
    public void Learning_and_safety_tasks_outweigh_play_tasks()
    {
        var active = SeededAchievements(out var levels).Where(a => a.IsActive).ToList();
        var play = active.Where(a => a.Kind == AchievementKind.Play).ToList();
        var other = active.Where(a => a.Kind != AchievementKind.Play).ToList();

        Assert.Contains(other, a => a.Code == AchievementCodes.VerifyRound && a.Kind == AchievementKind.Learn);
        Assert.Contains(other, a => a.Code == AchievementCodes.ReadRules && a.Kind == AchievementKind.Learn);
        Assert.Contains(other, a => a.Code == AchievementCodes.SetDailyLimit && a.Kind == AchievementKind.Safety);
        Assert.True(other.Count >= play.Count);
        Assert.True(other.Sum(a => a.XpReward) > play.Sum(a => a.XpReward));
        // Every learning / safety task gives more XP than any play task
        Assert.True(other.Min(a => a.XpReward) > play.Max(a => a.XpReward));

        // Playing alone can never reach the top level; completing everything does
        var top = levels.Max(l => l.XpRequired);
        Assert.True(play.Sum(a => a.XpReward) < top);
        Assert.Equal(top, active.Sum(a => a.XpReward));
    }

    [Fact]
    public void Safety_is_shown_first_then_learning_then_play()
    {
        Assert.True(AchievementRules.DisplayOrder(AchievementKind.Safety) < AchievementRules.DisplayOrder(AchievementKind.Learn));
        Assert.True(AchievementRules.DisplayOrder(AchievementKind.Learn) < AchievementRules.DisplayOrder(AchievementKind.Play));
    }

    // ---------- Rule 3: no play tasks for players who cannot play ----------

    [Theory]
    [InlineData(AchievementKind.Play, false, false, false)] // cannot play: a task that asks to play is hidden
    [InlineData(AchievementKind.Play, true, false, true)]   // ...unless already done (it asks nothing any more)
    [InlineData(AchievementKind.Learn, false, false, true)]
    [InlineData(AchievementKind.Safety, false, false, true)]
    [InlineData(AchievementKind.Play, false, true, true)]
    public void A_player_who_cannot_play_sees_no_task_asking_them_to_play(AchievementKind kind, bool unlocked, bool canPlay, bool shown)
    {
        Assert.Equal(shown, AchievementVisibility.IsShown(kind, unlocked, canPlay));
    }

    [Fact]
    public void Without_a_limit_a_player_can_bet()
    {
        Assert.True(new PlayerLimit().CanStillBetToday(betToday: 1_000_000m, smallestBet: 1m, Now));
    }

    [Fact]
    public void A_self_excluded_player_cannot_bet()
    {
        var limit = new PlayerLimit { SelfExcludedUntil = Now.AddDays(1) };

        Assert.False(limit.CanStillBetToday(betToday: 0, smallestBet: 1m, Now));
        Assert.True(limit.CanStillBetToday(betToday: 0, smallestBet: 1m, Now.AddDays(2))); // the pause is over
    }

    [Theory]
    [InlineData(100, 1, true)]       // 99 left
    [InlineData(100, 99, true)]      // exactly the smallest bet left
    [InlineData(100, 99.5, false)]   // 0.50 left, below the smallest bet
    [InlineData(100, 100, false)]    // used up
    public void The_daily_limit_is_used_up_when_less_than_the_smallest_bet_is_left(double limit, double betToday, bool canBet)
    {
        var playerLimit = new PlayerLimit { DailyBetLimit = (decimal)limit };

        Assert.Equal(canBet, playerLimit.CanStillBetToday((decimal)betToday, smallestBet: 1m, Now));
    }

    [Fact]
    public void A_waiting_limit_increase_does_not_count_before_its_time()
    {
        var limit = new PlayerLimit { DailyBetLimit = 50m, PendingDailyBetLimit = 500m, PendingLimitAppliesAt = Now.AddHours(10) };

        Assert.False(limit.CanStillBetToday(betToday: 50m, smallestBet: 1m, Now));
        Assert.True(limit.CanStillBetToday(betToday: 50m, smallestBet: 1m, Now.AddHours(11)));
    }
}
