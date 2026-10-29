using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Domain.Progress;

namespace Veltrix.Tests.Domain;

/// <summary>XP, levels and achievement rules (spec 4.6): pure Domain logic, no database.</summary>
public class ProgressionTests
{
    private static readonly Level[] Levels =
    {
        new() { Number = 1, Title = "Rookie", XpRequired = 0 },
        new() { Number = 2, Title = "Spotter", XpRequired = 100 },
        new() { Number = 3, Title = "Climber", XpRequired = 250 }
    };

    private static Bet Settled(BetStatus status, decimal amount = 10m, decimal? multiplier = null, decimal? auto = null) => new()
    {
        UserId = 1,
        Amount = amount,
        Status = status,
        AutoCashOutAt = auto,
        CashOutMultiplier = multiplier,
        Payout = status == BetStatus.CashedOut ? Math.Round(amount * multiplier!.Value, 2) : 0
    };

    [Fact]
    public void A_lost_bet_gives_1_XP_and_a_cash_out_3_XP_a_refund_none()
    {
        Assert.Equal(1, Experience.ForBet(Settled(BetStatus.Lost)));
        Assert.Equal(3, Experience.ForBet(Settled(BetStatus.CashedOut, multiplier: 2m)));
        Assert.Equal(0, Experience.ForBet(Settled(BetStatus.Refunded)));
    }

    [Fact]
    public void Settling_a_bet_updates_totals_records_and_XP()
    {
        var stats = new PlayerStats();

        stats.AddSettledBet(Settled(BetStatus.CashedOut, amount: 100m, multiplier: 2.5m), TestData.Now);
        stats.AddSettledBet(Settled(BetStatus.Lost, amount: 50m), TestData.Now);

        Assert.Equal(2, stats.TotalBets);
        Assert.Equal(1, stats.CashOutCount);
        Assert.Equal(150m, stats.TotalWagered);
        Assert.Equal(250m, stats.TotalPayout);
        Assert.Equal(150m, stats.BiggestWin);   // payout − stake
        Assert.Equal(2.5m, stats.BiggestMultiplier);
        Assert.Equal(4, stats.Xp);              // 3 + 1
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(249, 2)]
    [InlineData(250, 3)]
    [InlineData(10_000, 3)] // above the last threshold: still the top level
    public void The_level_is_the_highest_one_whose_XP_is_reached(int xp, int expectedLevel)
    {
        Assert.Equal(expectedLevel, Experience.LevelFor(xp, Levels).Number);
    }

    [Fact]
    public void UpdateLevel_reports_a_level_up_once_and_never_goes_down()
    {
        var stats = new PlayerStats { Xp = 120, LevelNumber = 1 };

        Assert.True(stats.UpdateLevel(Levels));
        Assert.Equal(2, stats.LevelNumber);
        Assert.False(stats.UpdateLevel(Levels)); // same XP: no new level-up

        stats.LevelNumber = 3;                    // e.g. a higher level from before
        Assert.False(stats.UpdateLevel(Levels));
        Assert.Equal(3, stats.LevelNumber);
    }

    [Fact]
    public void XP_cannot_be_taken_away()
    {
        Assert.Throws<DomainException>(() => new PlayerStats().AddXp(-5, TestData.Now));
    }

    [Fact]
    public void The_next_level_is_null_at_the_top()
    {
        Assert.Equal(2, Experience.NextLevel(1, Levels)!.Number);
        Assert.Null(Experience.NextLevel(3, Levels));
    }

    [Theory]
    [InlineData(AchievementCodes.FirstBet, 1, 0, 0, 0, true)]
    [InlineData(AchievementCodes.FirstBet, 0, 0, 0, 0, false)]
    [InlineData(AchievementCodes.FirstCashOut, 5, 1, 0, 0, true)]
    [InlineData(AchievementCodes.FirstCashOut, 5, 0, 0, 0, false)]
    [InlineData(AchievementCodes.CashOut10x, 5, 1, 10.0, 0, true)]
    [InlineData(AchievementCodes.CashOut10x, 5, 1, 9.99, 0, false)]
    [InlineData(AchievementCodes.Bets100, 100, 0, 0, 0, true)]
    [InlineData(AchievementCodes.Bets100, 99, 0, 0, 0, false)]
    [InlineData(AchievementCodes.BigWin1000, 5, 1, 0, 1000, true)]
    [InlineData(AchievementCodes.BigWin1000, 5, 1, 0, 999.99, false)]
    [InlineData(AchievementCodes.CashOuts50, 80, 50, 0, 0, true)]
    [InlineData(AchievementCodes.CashOuts50, 80, 49, 0, 0, false)]
    public void Achievements_follow_the_running_totals(
        string code, int bets, int cashOuts, double bestMultiplier, double biggestWin, bool earned)
    {
        var stats = new PlayerStats
        {
            TotalBets = bets,
            CashOutCount = cashOuts,
            BiggestMultiplier = (decimal)bestMultiplier,
            BiggestWin = (decimal)biggestWin
        };

        Assert.Equal(earned, AchievementRules.IsEarned(code, stats, Array.Empty<Bet>()));
    }

    [Fact]
    public void Auto_cash_out_counts_only_when_the_auto_target_paid_the_bet()
    {
        var stats = new PlayerStats();
        var paidByAuto = Settled(BetStatus.CashedOut, multiplier: 2m, auto: 2m);
        var manualBeforeTarget = Settled(BetStatus.CashedOut, multiplier: 1.5m, auto: 2m);
        var lostWithTarget = Settled(BetStatus.Lost, auto: 2m);

        Assert.True(AchievementRules.IsEarned(AchievementCodes.AutoCashOut, stats, new[] { paidByAuto }));
        Assert.False(AchievementRules.IsEarned(AchievementCodes.AutoCashOut, stats, new[] { manualBeforeTarget, lostWithTarget }));
    }

    [Fact]
    public void Progress_counts_towards_the_target_and_stops_there()
    {
        var stats = new PlayerStats { TotalBets = 37, BiggestMultiplier = 25m };

        Assert.Equal(new AchievementProgress(37, 100), AchievementRules.Progress(AchievementCodes.Bets100, stats));
        Assert.Equal(new AchievementProgress(10m, 10m), AchievementRules.Progress(AchievementCodes.CashOut10x, stats));
        Assert.Null(AchievementRules.Progress(AchievementCodes.AutoCashOut, stats));
        Assert.Equal(0, AchievementRules.Progress(AchievementCodes.FirstBet, null)!.Value.Current); // no stats yet
    }

    [Fact]
    public void Every_seeded_achievement_code_has_a_rule()
    {
        var codes = typeof(AchievementCodes).GetFields().Select(f => (string)f.GetValue(null)!);
        var stats = new PlayerStats { TotalBets = 1000, CashOutCount = 1000, BiggestMultiplier = 1000m, BiggestWin = 100_000m };
        var autoWin = new[] { Settled(BetStatus.CashedOut, multiplier: 2m, auto: 2m) };

        Assert.All(codes, code => Assert.True(AchievementRules.IsEarned(code, stats, autoWin), code));
    }
}
