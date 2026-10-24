using Veltrix.Domain.Common;
using Veltrix.Domain.Crash;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;

namespace Veltrix.Tests.Domain;

public class BetTests
{
    private static readonly DateTime Now = TestData.Now;

    [Fact]
    public void Place_WhileBettingIsOpen_CreatesAnActiveBet()
    {
        var bet = Bet.Place(TestData.OpenRound(), TestData.Game(), userId: 5, amount: 25m, slot: 1, autoCashOutAt: 2.5m, Now);

        Assert.Equal(BetStatus.Active, bet.Status);
        Assert.Equal(25m, bet.Amount);
        Assert.Equal(2.5m, bet.AutoCashOutAt);
    }

    [Fact]
    public void Place_AfterBettingClosed_IsRejected()
    {
        var round = TestData.OpenRound();
        round.CloseBetting(Now);

        var ex = Assert.Throws<DomainException>(() => Bet.Place(round, TestData.Game(), 1, 10m, 1, null, Now));
        Assert.Contains("closed", ex.Message);
    }

    [Fact]
    public void Place_DuringTheRunningPhase_IsRejected()
    {
        Assert.Throws<DomainException>(() => Bet.Place(TestData.RunningRound(), TestData.Game(), 1, 10m, 1, null, Now));
    }

    [Theory]
    [InlineData(0.5)]   // below MinBet
    [InlineData(1001)]  // above MaxBet
    public void Place_OutsideTheGameLimits_IsRejected(double amount)
    {
        Assert.Throws<DomainException>(() =>
            Bet.Place(TestData.OpenRound(), TestData.Game(minBet: 1m, maxBet: 1000m), 1, (decimal)amount, 1, null, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Place_WithAnInvalidSlot_IsRejected(int slot)
    {
        Assert.Throws<DomainException>(() => Bet.Place(TestData.OpenRound(), TestData.Game(), 1, 10m, slot, null, Now));
    }

    [Fact]
    public void Place_WithAutoCashOutBelow1_01_IsRejected()
    {
        Assert.Throws<DomainException>(() => Bet.Place(TestData.OpenRound(), TestData.Game(), 1, 10m, 1, 1.00m, Now));
    }

    [Fact]
    public void CashOut_WhileRunning_PaysAtTheServerMultiplier()
    {
        var round = TestData.RunningRound(crashPoint: 5.00m);
        var bet = TestData.ActiveBet(round, amount: 100m);
        var at = Now.AddSeconds(10);
        var expectedMultiplier = MultiplierCurve.At(TimeSpan.FromSeconds(10)); // ~1.82x

        var payout = bet.CashOut(round, at);

        Assert.Equal(BetStatus.CashedOut, bet.Status);
        Assert.Equal(expectedMultiplier, bet.CashOutMultiplier);
        Assert.Equal(Math.Round(100m * expectedMultiplier, 2, MidpointRounding.ToZero), payout);
    }

    [Fact]
    public void CashOut_AfterTheCrashMoment_IsRejected_EvenIfTheEngineHasNotSettledYet()
    {
        var round = TestData.RunningRound(crashPoint: 1.50m);
        var bet = TestData.ActiveBet(round);
        var afterCrash = Now + MultiplierCurve.TimeToReach(1.50m) + TimeSpan.FromMilliseconds(50);

        var ex = Assert.Throws<DomainException>(() => bet.CashOut(round, afterCrash));

        Assert.Contains("Too late", ex.Message);
        Assert.Equal(BetStatus.Active, bet.Status);
    }

    [Fact]
    public void CashOut_DuringBetting_IsRejected()
    {
        var round = TestData.OpenRound();
        var bet = TestData.ActiveBet(round);

        Assert.Throws<DomainException>(() => bet.CashOut(round, Now));
    }

    [Fact]
    public void CashOut_Twice_IsRejected()
    {
        var round = TestData.RunningRound(crashPoint: 10m);
        var bet = TestData.ActiveBet(round);
        bet.CashOut(round, Now.AddSeconds(2));

        Assert.Throws<DomainException>(() => bet.CashOut(round, Now.AddSeconds(3)));
    }

    [Fact]
    public void CashOutAt_RoundsThePayoutDown_ToTheCent()
    {
        var bet = TestData.ActiveBet(TestData.RunningRound(), amount: 3.33m);

        var payout = bet.CashOutAt(1.37m, Now); // 3.33 * 1.37 = 4.5621

        Assert.Equal(4.56m, payout);
    }

    [Fact]
    public void MarkLost_And_MarkRefunded_OnlyWorkOnActiveBets()
    {
        var round = TestData.RunningRound();
        var lost = TestData.ActiveBet(round);
        var refunded = TestData.ActiveBet(round);

        lost.MarkLost();
        refunded.MarkRefunded();

        Assert.Equal(BetStatus.Lost, lost.Status);
        Assert.Equal(0m, lost.Payout);
        Assert.Equal(BetStatus.Refunded, refunded.Status);
        Assert.Equal(refunded.Amount, refunded.Payout);

        Assert.Throws<DomainException>(() => lost.MarkRefunded());
        Assert.Throws<DomainException>(() => refunded.CashOutAt(2m, Now));
    }
}
