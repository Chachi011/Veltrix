using Veltrix.Domain.Common;
using Veltrix.Domain.Enums;
using Veltrix.Domain.Crash;

namespace Veltrix.Domain.Entities;

public class GameRound
{
    /// <summary>
    /// The ONLY allowed state changes. Everything else throws InvalidRoundTransitionException.
    /// Any unfinished round may be cancelled; Crashed must always be followed by Finished.
    /// </summary>
    private static readonly Dictionary<RoundState, RoundState[]> AllowedTransitions = new()
    {
        [RoundState.Waiting] = new[] { RoundState.BettingOpen, RoundState.Cancelled },
        [RoundState.BettingOpen] = new[] { RoundState.BettingClosed, RoundState.Cancelled },
        [RoundState.BettingClosed] = new[] { RoundState.Running, RoundState.Cancelled },
        [RoundState.Running] = new[] { RoundState.Crashed, RoundState.Cancelled },
        [RoundState.Crashed] = new[] { RoundState.Finished },
        [RoundState.Finished] = Array.Empty<RoundState>(),
        [RoundState.Cancelled] = Array.Empty<RoundState>()
    };

    public int Id { get; set; }
    public int GameId { get; set; }
    public long RoundNumber { get; set; }
    public RoundState State { get; set; } = RoundState.Waiting;

    /// <summary>Secret random seed. Revealed to players only after the round has crashed.</summary>
    public string ServerSeed { get; set; } = string.Empty;

    /// <summary>SHA-256(ServerSeed). Published BEFORE the round, so the result cannot be changed later.</summary>
    public string ServerSeedHash { get; set; } = string.Empty;

    /// <summary>Calculated from ServerSeed before the round starts.</summary>
    public decimal CrashPoint { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? BettingEndsAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CrashedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public Game? Game { get; set; }
    public ICollection<Bet> Bets { get; set; } = new List<Bet>();
    public ICollection<GameRoundEvent> Events { get; set; } = new List<GameRoundEvent>();

    /// <summary>True once the result may be shown to players (seed + crash point).</summary>
    public bool IsResultRevealed => State is RoundState.Crashed or RoundState.Finished or RoundState.Cancelled;

    public bool IsFinal => State is RoundState.Finished or RoundState.Cancelled;

    /// <summary>New round with its seed, hash and crash point fixed in advance.</summary>
    public static GameRound Create(int gameId, long roundNumber, DateTime now)
    {
        var seed = ProvablyFair.GenerateServerSeed();
        var round = new GameRound
        {
            GameId = gameId,
            RoundNumber = roundNumber,
            State = RoundState.Waiting,
            ServerSeed = seed,
            ServerSeedHash = ProvablyFair.HashSeed(seed),
            CrashPoint = ProvablyFair.CalculateCrashPoint(seed),
            CreatedAt = now
        };

        round.Events.Add(new GameRoundEvent { FromState = null, ToState = RoundState.Waiting, OccurredAt = now, Note = "Round created" });
        return round;
    }

    public static bool CanTransition(RoundState from, RoundState to) => AllowedTransitions[from].Contains(to);

    public void OpenBetting(DateTime now, TimeSpan bettingDuration)
    {
        TransitionTo(RoundState.BettingOpen, now);
        BettingEndsAt = now.Add(bettingDuration);
    }

    public void CloseBetting(DateTime now) => TransitionTo(RoundState.BettingClosed, now);

    public void Start(DateTime now)
    {
        TransitionTo(RoundState.Running, now);
        StartedAt = now;
    }

    public void Crash(DateTime now)
    {
        TransitionTo(RoundState.Crashed, now, $"Crashed at {CrashPoint:0.00}x");
        CrashedAt = now;
    }

    public void Finish(DateTime now)
    {
        TransitionTo(RoundState.Finished, now);
        EndedAt = now;
    }

    public void Cancel(DateTime now, string reason)
    {
        TransitionTo(RoundState.Cancelled, now, reason);
        EndedAt = now;
    }

    /// <summary>Is a new bet allowed at this moment?</summary>
    public bool AcceptsBets(DateTime now) => State == RoundState.BettingOpen && BettingEndsAt > now;

    /// <summary>The server's multiplier at this moment (1.00 if the round is not running).</summary>
    public decimal MultiplierAt(DateTime now) =>
        State == RoundState.Running && StartedAt.HasValue
            ? MultiplierCurve.At(now - StartedAt.Value)
            : 1.00m;

    /// <summary>The moment the multiplier reaches the crash point.</summary>
    public DateTime? CrashesAt => StartedAt?.Add(MultiplierCurve.TimeToReach(CrashPoint));

    private void TransitionTo(RoundState next, DateTime now, string? note = null)
    {
        if (!CanTransition(State, next))
            throw new InvalidRoundTransitionException(State, next);

        Events.Add(new GameRoundEvent { FromState = State, ToState = next, OccurredAt = now, Note = note });
        State = next;
    }
}
