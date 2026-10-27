using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;

namespace Veltrix.Tests.Domain;

/// <summary>Self-exclusion: 24 h / 7 d / 30 d, blocks betting, can be extended but never shortened.</summary>
public class SelfExclusionTests
{
    private static readonly DateTime Now = TestData.Now;

    private static PlayerLimit Limit() => new() { UserId = 7 };

    [Theory]
    [InlineData(SelfExclusionPeriod.Hours24, 24)]
    [InlineData(SelfExclusionPeriod.Days7, 7 * 24)]
    [InlineData(SelfExclusionPeriod.Days30, 30 * 24)]
    public void Start_PausesBettingForThePeriod_AndReturnsTheHistoryRow(SelfExclusionPeriod period, int hours)
    {
        var limit = Limit();

        var row = limit.StartSelfExclusion(period, Now);

        Assert.Equal(Now.AddHours(hours), limit.SelfExcludedUntil);
        Assert.Equal(7, row.UserId);
        Assert.Equal(period, row.Period);
        Assert.Equal(Now, row.StartsAt);
        Assert.Equal(Now.AddHours(hours), row.EndsAt);
    }

    [Fact]
    public void ASelfExcludedPlayer_CannotBet_EvenWithoutADailyLimit()
    {
        var limit = Limit();
        limit.StartSelfExclusion(SelfExclusionPeriod.Hours24, Now);

        var error = Assert.Throws<SelfExcludedException>(() => limit.EnsureCanBet(1m, betToday: 0m, Now.AddHours(23)));
        Assert.Equal(Now.AddHours(24), error.Until);
    }

    [Fact]
    public void WhenTheExclusionEnds_BettingIsAllowedAgain()
    {
        var limit = Limit();
        limit.StartSelfExclusion(SelfExclusionPeriod.Hours24, Now);

        Assert.False(limit.IsSelfExcluded(Now.AddHours(24)));
        limit.EnsureCanBet(1m, betToday: 0m, Now.AddHours(24));
    }

    [Fact]
    public void AnActiveExclusion_CannotBeShortened()
    {
        var limit = Limit();
        limit.StartSelfExclusion(SelfExclusionPeriod.Days30, Now);

        Assert.Throws<DomainException>(() => limit.StartSelfExclusion(SelfExclusionPeriod.Hours24, Now.AddHours(1)));
        Assert.Equal(Now.AddDays(30), limit.SelfExcludedUntil);
    }

    [Fact]
    public void AnActiveExclusion_CanBeExtended()
    {
        var limit = Limit();
        limit.StartSelfExclusion(SelfExclusionPeriod.Hours24, Now);

        limit.StartSelfExclusion(SelfExclusionPeriod.Days7, Now.AddHours(2));

        Assert.Equal(Now.AddHours(2).AddDays(7), limit.SelfExcludedUntil);
    }

    [Fact]
    public void AfterAnExclusionEnded_ANewOneCanStart()
    {
        var limit = Limit();
        limit.StartSelfExclusion(SelfExclusionPeriod.Hours24, Now);

        limit.StartSelfExclusion(SelfExclusionPeriod.Hours24, Now.AddDays(3));

        Assert.Equal(Now.AddDays(3).AddHours(24), limit.SelfExcludedUntil);
    }

    [Fact]
    public void SelfExclusion_IsCheckedBeforeTheDailyLimit()
    {
        var limit = new PlayerLimit { UserId = 7, DailyBetLimit = 10m };
        limit.StartSelfExclusion(SelfExclusionPeriod.Days7, Now);

        // The clearer reason wins: the player is paused, not "over the limit"
        Assert.Throws<SelfExcludedException>(() => limit.EnsureCanBet(50m, betToday: 0m, Now));
    }
}
