using Veltrix.Application.DTOs.Rounds;

namespace Veltrix.Application.Common.Interfaces;

/// <summary>Lets an Operator request stop the round the engine is currently running.</summary>
public interface IRoundEngineControl
{
    /// <summary>Cancels the round and refunds all active bets. Completes when the refunds are saved.</summary>
    Task<RoundCancellationDto> CancelRoundAsync(int roundId, int? actorUserId, string reason, CancellationToken ct = default);
}
