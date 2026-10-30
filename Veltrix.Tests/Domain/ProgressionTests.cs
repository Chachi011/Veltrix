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
        new() { Number = 2, Title = "Spotter", XpRequired = 10 },
        new() { Number = 3, Title = "Climber", XpRequired = 30 }
    };

    internal static Bet Settled(BetStatus status, decimal amount = 10m, decimal? multiplier = null, decimal? auto = null) => new()
    {
        UserId = 1,
        Amount = amount,
        Status = status,
        AutoCashOutAt = auto,
        CashOutMultiplier = multiplier,
        Payout = status == BetStatus.CashedOut ? Math.Round(amount * multiplier!.Value, 2) : 0
    };

    [Fact]
    public void Settling_a_bet_updates_totals_and_records_but_gives_no_XP()
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
        Assert.Equal(0, stats.Xp);              // betting more never gives progress; XP comes only from achievements
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(29, 2)]
    [InlineData(30, 3)]
    [InlineData(10_000, 3)] // above the last threshold: still the top level
    public void The_level_is_the_highest_one_whose_XP_is_reached(int xp, int expectedLevel)
    {
        Assert.Equal(expectedLevel, Experience.LevelFor(xp, Levels).Number);
    }

    [Fact]
    public void UpdateLevel_reports_a_level_up_once_and_never_goes_down()
    {
        var stats = new PlayerStats { Xp = 12, LevelNumber = 1 };

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
    [InlineData(AchievementCodes.FirstBet, 1, 0, true)]
    [InlineData(AchievementCodes.FirstBet, 0, 0, false)]
    [InlineData(AchievementCodes.FirstCashOut, 5, 1, true)]
    [InlineData(AchievementCodes.FirstCashOut, 5, 0, false)]
    public void Try_the_game_and_try_cash_out_are_one_time_steps(string code, int bets, int cashOuts, bool earned)
    {
        var stats = new PlayerStats { TotalBets = bets, CashOutCount = cashOuts };

        Assert.Equal(earned, AchievementRules.IsEarnedAtSettlement(code, stats, Array.Empty<Bet>()));
    }

    [Fact]
    public void Plan_ahead_counts_any_settled_bet_with_an_auto_target_won_or_lost()
    {
        var stats = new PlayerStats();

        Assert.True(AchievementRules.IsEarnedAtSettlement(AchievementCodes.AutoCashOut, stats, new[] { Settled(BetStatus.Lost, auto: 2m) }));
        Assert.True(AchievementRules.IsEarnedAtSettlement(AchievementCodes.AutoCashOut, stats,
            new[] { Settled(BetStatus.CashedOut, multiplier: 1.5m, auto: 2m) })); // cashed out by hand before the target: still planned
        Assert.False(AchievementRules.IsEarnedAtSettlement(AchievementCodes.AutoCashOut, stats,
            new[] { Settled(BetStatus.CashedOut, multiplier: 2m), Settled(BetStatus.Refunded, auto: 2m) }));
    }

    [Fact]
    public void Progress_is_a_single_step()
    {
        var stats = new PlayerStats { TotalBets = 37, CashOutCount = 0 };

        Assert.Equal(new AchievementProgress(1, 1), AchievementRules.Progress(AchievementCodes.FirstBet, stats));
        Assert.Equal(new AchievementProgress(0, 1), AchievementRules.Progress(AchievementCodes.FirstCashOut, stats));
        Assert.Null(AchievementRules.Progress(AchievementCodes.AutoCashOut, stats));
        Assert.Equal(0, AchievementRules.Progress(AchievementCodes.FirstBet, null)!.Value.Current); // no stats yet
    }

    [Fact]
    public void Reading_the_end_of_the_rules_page_completes_Know_the_rules()
    {
        Assert.Equal(AchievementCodes.ReadRules, AchievementRules.CompletedByReading("no-winning-strategy"));
        Assert.Null(AchievementRules.CompletedByReading("rules"));
        Assert.Null(AchievementRules.CompletedByReading("anything-else"));
    }

    [Fact]
    public void Every_settlement_code_has_a_rule()
    {
        var stats = new PlayerStats { TotalBets = 1, CashOutCount = 1 };
        var autoBet = new[] { Settled(BetStatus.CashedOut, multiplier: 2m, auto: 2m) };

        Assert.All(AchievementCodes.CompletedBySettlement,
            code => Assert.True(AchievementRules.IsEarnedAtSettlement(code, stats, autoBet), code));
    }
}
