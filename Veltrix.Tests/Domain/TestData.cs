using Veltrix.Domain.Entities;

namespace Veltrix.Tests.Domain;

/// <summary>Small builders so each test only states what matters to it.</summary>
internal static class TestData
{
    public static readonly DateTime Now = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

    public static Game Game(decimal minBet = 1m, decimal maxBet = 1000m) => new()
    {
        Id = 1,
        Name = "Crash",
        Slug = "crash",
        MinBet = minBet,
        MaxBet = maxBet,
        BettingDurationSec = 10,
        PauseBetweenRoundsSec = 5
    };

    /// <summary>A round with a known crash point, in the BettingOpen state.</summary>
    public static GameRound OpenRound(decimal crashPoint = 2.00m)
    {
        var round = GameRound.Create(gameId: 1, roundNumber: 1, Now);
        round.Id = 10;
        round.CrashPoint = crashPoint;
        round.OpenBetting(Now, TimeSpan.FromSeconds(10));
        return round;
    }

    /// <summary>A round that started running at <see cref="Now"/>.</summary>
    public static GameRound RunningRound(decimal crashPoint = 2.00m)
    {
        var round = OpenRound(crashPoint);
        round.CloseBetting(Now);
        round.Start(Now);
        return round;
    }

    public static Bet ActiveBet(GameRound round, decimal amount = 100m) =>
        Bet.Place(OpenRoundFor(round), Game(), userId: 1, amount, slot: 1, autoCashOutAt: null, Now);

    public static Wallet Wallet(decimal balance) => new() { Id = 1, UserId = 1, Balance = balance };

    // Bet.Place needs an open round; reuse the id of the round under test
    private static GameRound OpenRoundFor(GameRound round)
    {
        var open = OpenRound(round.CrashPoint);
        open.Id = round.Id;
        return open;
    }
}
