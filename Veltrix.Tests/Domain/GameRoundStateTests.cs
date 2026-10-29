using Veltrix.Domain.Common;
using Veltrix.Domain.Crash;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;

namespace Veltrix.Tests.Domain;

public class GameRoundStateTests
{
    private static readonly DateTime Now = TestData.Now;

    [Fact]
    public void Create_FixesSeedHashAndCrashPoint_BeforeTheRound()
    {
        var round = GameRound.Create(gameId: 1, roundNumber: 7, Now);

        Assert.Equal(RoundState.Waiting, round.State);
        Assert.Equal(ProvablyFair.HashSeed(round.ServerSeed), round.ServerSeedHash);
        Assert.Equal(ProvablyFair.CalculateCrashPoint(round.ServerSeed), round.CrashPoint);
        Assert.False(round.IsResultRevealed);
    }

    [Fact]
    public void FullLifecycle_FollowsTheSpecOrder_AndRecordsEveryTransition()
    {
        var round = GameRound.Create(1, 1, Now);

        round.OpenBetting(Now, TimeSpan.FromSeconds(10));
        round.CloseBetting(Now.AddSeconds(10));
        round.Start(Now.AddSeconds(11));
        round.Crash(Now.AddSeconds(20));
        round.Finish(Now.AddSeconds(21));

        Assert.Equal(RoundState.Finished, round.State);
        Assert.Equal(
            new[] { RoundState.Waiting, RoundState.BettingOpen, RoundState.BettingClosed, RoundState.Running, RoundState.Crashed, RoundState.Finished },
            round.Events.Select(e => e.ToState));
        Assert.True(round.IsResultRevealed);
    }

    [Theory]
    [InlineData(RoundState.Waiting, RoundState.Running)]
    [InlineData(RoundState.Waiting, RoundState.Crashed)]
    [InlineData(RoundState.BettingOpen, RoundState.Running)]
    [InlineData(RoundState.BettingClosed, RoundState.BettingOpen)]
    [InlineData(RoundState.Running, RoundState.BettingOpen)]
    [InlineData(RoundState.Running, RoundState.Finished)]
    [InlineData(RoundState.Crashed, RoundState.Cancelled)]
    [InlineData(RoundState.Crashed, RoundState.Running)]
    [InlineData(RoundState.Finished, RoundState.Cancelled)]
    [InlineData(RoundState.Cancelled, RoundState.BettingOpen)]
    public void IllegalTransitions_AreRejected(RoundState from, RoundState to)
    {
        Assert.False(GameRound.CanTransition(from, to));
    }

    [Fact]
    public void Start_WithoutClosingBetting_Throws()
    {
        var round = TestData.OpenRound();

        var ex = Assert.Throws<InvalidRoundTransitionException>(() => round.Start(Now));
        Assert.Equal(RoundState.BettingOpen, ex.From);
        Assert.Equal(RoundState.Running, ex.To);
        Assert.Equal(RoundState.BettingOpen, round.State); // unchanged
    }

    [Theory]
    [InlineData(RoundState.Waiting)]
    [InlineData(RoundState.BettingOpen)]
    [InlineData(RoundState.BettingClosed)]
    [InlineData(RoundState.Running)]
    public void AnyUnfinishedRound_CanBeCancelled(RoundState state)
    {
        Assert.True(GameRound.CanTransition(state, RoundState.Cancelled));
    }

    [Fact]
    public void CrashedRound_CannotBeCancelled_ResultIsFinal()
    {
        var round = TestData.RunningRound();
        round.Crash(Now);

        Assert.Throws<InvalidRoundTransitionException>(() => round.Cancel(Now, "too late"));
    }

    [Fact]
    public void AcceptsBets_OnlyWhileBettingIsOpenAndBeforeTheDeadline()
    {
        var round = TestData.OpenRound(); // betting ends at Now + 10 s

        Assert.True(round.AcceptsBets(Now.AddSeconds(9)));
        Assert.False(round.AcceptsBets(Now.AddSeconds(10)));

        round.CloseBetting(Now.AddSeconds(10));
        Assert.False(round.AcceptsBets(Now.AddSeconds(5)));
    }

    [Fact]
    public void MultiplierAt_UsesTheServerClockSinceStart()
    {
        var round = TestData.RunningRound(crashPoint: 50m);

        Assert.Equal(1.00m, round.MultiplierAt(Now));
        Assert.Equal(MultiplierCurve.At(TimeSpan.FromSeconds(5)), round.MultiplierAt(Now.AddSeconds(5)));
    }
}
