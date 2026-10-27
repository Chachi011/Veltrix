using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;

namespace Veltrix.Tests.Domain;

/// <summary>Daily bet limit: a stricter limit applies at once, a looser one only after 24 hours.</summary>
public class PlayerLimitTests
{
    private static readonly DateTime Now = TestData.Now;

    private static PlayerLimit Limit(decimal? daily = null) => new() { UserId = 1, DailyBetLimit = daily };

    [Fact]
    public void WithoutALimit_AnyAmountIsAllowed()
    {
        var limit = Limit();

        limit.EnsureCanBet(1_000_000m, betToday: 5_000_000m, Now);
    }

    [Fact]
    public void BetsUpToExactlyTheLimit_AreAllowed()
    {
        var limit = Limit(100m);

        limit.EnsureCanBet(40m, betToday: 60m, Now);
    }

    [Fact]
    public void TheBetThatWouldGoOverTheLimit_IsRefused()
    {
        var limit = Limit(100m);

        var error = Assert.Throws<DailyBetLimitExceededException>(() => limit.EnsureCanBet(40.01m, betToday: 60m, Now));
        Assert.Contains("60.00 of 100.00", error.Message);
    }

    [Fact]
    public void SettingTheFirstLimit_AppliesImmediately()
    {
        var limit = Limit();

        limit.ChangeDailyBetLimit(200m, Now);

        Assert.Equal(200m, limit.DailyBetLimit);
        Assert.False(limit.HasPendingLimitChange(Now));
    }

    [Fact]
    public void LoweringTheLimit_AppliesImmediately()
    {
        var limit = Limit(100m);

        limit.ChangeDailyBetLimit(50m, Now);

        Assert.Equal(50m, limit.EffectiveDailyBetLimit(Now));
        Assert.Throws<DailyBetLimitExceededException>(() => limit.EnsureCanBet(60m, betToday: 0m, Now));
    }

    [Fact]
    public void RaisingTheLimit_WaitsTwentyFourHours()
    {
        var limit = Limit(100m);

        limit.ChangeDailyBetLimit(500m, Now);

        // Still the old limit right now...
        Assert.Equal(100m, limit.EffectiveDailyBetLimit(Now));
        Assert.True(limit.HasPendingLimitChange(Now));
        Assert.Equal(Now.AddHours(24), limit.PendingLimitAppliesAt);
        Assert.Throws<DailyBetLimitExceededException>(() => limit.EnsureCanBet(150m, betToday: 0m, Now.AddHours(23)));

        // ...and the new one once the 24 hours have passed
        Assert.Equal(500m, limit.EffectiveDailyBetLimit(Now.AddHours(24)));
        limit.EnsureCanBet(150m, betToday: 0m, Now.AddHours(24));
    }

    [Fact]
    public void RemovingTheLimit_AlsoWaitsTwentyFourHours()
    {
        var limit = Limit(100m);

        limit.ChangeDailyBetLimit(null, Now);

        Assert.Equal(100m, limit.EffectiveDailyBetLimit(Now));
        Assert.True(limit.HasPendingLimitChange(Now));
        Assert.Null(limit.PendingDailyBetLimit);
        Assert.Null(limit.EffectiveDailyBetLimit(Now.AddHours(24)));
    }

    [Fact]
    public void AskingAgainForTheSameIncrease_DoesNotRestartTheWait()
    {
        var limit = Limit(100m);
        limit.ChangeDailyBetLimit(500m, Now);

        // e.g. the player only changed the session reminder and the form sent the same limit again
        limit.ChangeDailyBetLimit(500m, Now.AddHours(10));

        Assert.Equal(Now.AddHours(24), limit.PendingLimitAppliesAt);
    }

    [Fact]
    public void AskingForADifferentIncrease_StartsANewWait()
    {
        var limit = Limit(100m);
        limit.ChangeDailyBetLimit(500m, Now);

        limit.ChangeDailyBetLimit(300m, Now.AddHours(10));

        Assert.Equal(300m, limit.PendingDailyBetLimit);
        Assert.Equal(Now.AddHours(34), limit.PendingLimitAppliesAt);
    }

    [Fact]
    public void LoweringWhileAnIncreaseIsWaiting_CancelsTheIncrease()
    {
        var limit = Limit(100m);
        limit.ChangeDailyBetLimit(500m, Now);

        limit.ChangeDailyBetLimit(80m, Now.AddHours(1));

        Assert.Equal(80m, limit.DailyBetLimit);
        Assert.False(limit.HasPendingLimitChange(Now.AddHours(1)));
        Assert.Null(limit.PendingLimitAppliesAt);
        Assert.Equal(80m, limit.EffectiveDailyBetLimit(Now.AddDays(2)));
    }

    [Fact]
    public void ADueIncrease_IsMovedIntoTheLimit_OnTheNextChange()
    {
        var limit = Limit(100m);
        limit.ChangeDailyBetLimit(500m, Now);

        limit.ChangeDailyBetLimit(500m, Now.AddHours(25));

        Assert.Equal(500m, limit.DailyBetLimit);
        Assert.Null(limit.PendingLimitAppliesAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ZeroOrNegativeLimit_IsRefused(double value)
    {
        var limit = Limit(100m);

        Assert.Throws<DomainException>(() => limit.ChangeDailyBetLimit((decimal)value, Now));
        Assert.Equal(100m, limit.DailyBetLimit);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    [InlineData(600)]
    public void SessionReminder_InRange_IsSaved(int minutes)
    {
        var limit = Limit();

        limit.ChangeSessionReminder(minutes);

        Assert.Equal(minutes, limit.SessionReminderMin);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(601)]
    public void SessionReminder_OutOfRange_IsRefused(int minutes)
    {
        var limit = Limit();

        Assert.Throws<DomainException>(() => limit.ChangeSessionReminder(minutes));
        Assert.Equal(PlayerLimit.DefaultSessionReminderMin, limit.SessionReminderMin);
    }
}
