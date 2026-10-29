using Veltrix.Domain.Enums;

namespace Veltrix.Application.DTOs.Realtime;

// Payloads pushed by SignalR (hub /hubs/crash). Names match the spec's event list.

/// <summary>
/// Sent on every state change. StartedAt lets the browser animate the curve with the same formula as the server.
/// ServerTime is the server's clock when the event was sent: the browser uses it to correct its own
/// (possibly wrong) clock for the betting countdown.
/// </summary>
public sealed record RoundStateChangedEvent(
    int RoundId,
    long RoundNumber,
    RoundState State,
    string ServerSeedHash,
    DateTime? BettingEndsAt,
    DateTime? StartedAt,
    DateTime ServerTime);

/// <summary>Sent every 100 ms while the round is running.</summary>
public sealed record MultiplierTickEvent(int RoundId, decimal Multiplier, long ElapsedMs);

/// <summary>The crash, together with the revealed seed for verification.</summary>
public sealed record RoundCrashedEvent(int RoundId, decimal CrashPoint, string ServerSeed);

public sealed record BetPlacedEvent(int RoundId, string Nickname, decimal Amount);

public sealed record PlayerCashedOutEvent(int RoundId, string Nickname, decimal Multiplier, decimal Payout);

/// <summary>Sent only to the owner of the wallet.</summary>
public sealed record BalanceUpdatedEvent(decimal Balance);

public sealed record OnlineCountEvent(int Count);
