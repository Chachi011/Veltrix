using Veltrix.Application.DTOs.Realtime;
using Veltrix.Application.DTOs.Rounds;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;

namespace Veltrix.Infrastructure.Services;

internal static class GameplayMapping
{
    /// <summary>
    /// The ONE place that decides what a client may see of a round.
    /// Seed and crash point stay hidden until the round is over.
    /// </summary>
    public static RoundDto ToDto(this GameRound round, string gameSlug, DateTime now) => new()
    {
        Id = round.Id,
        GameSlug = gameSlug,
        RoundNumber = round.RoundNumber,
        State = round.State,
        ServerSeedHash = round.ServerSeedHash,
        ServerSeed = round.IsResultRevealed ? round.ServerSeed : null,
        CrashPoint = round.IsResultRevealed ? round.CrashPoint : null,
        CurrentMultiplier = round.State == RoundState.Running ? round.MultiplierAt(now) : null,
        CreatedAt = round.CreatedAt,
        BettingEndsAt = round.BettingEndsAt,
        StartedAt = round.StartedAt,
        CrashedAt = round.CrashedAt,
        EndedAt = round.EndedAt
    };

    public static RoundStateChangedEvent ToStateEvent(this GameRound round) =>
        new(round.Id, round.RoundNumber, round.State, round.ServerSeedHash, round.BettingEndsAt, round.StartedAt);
}
