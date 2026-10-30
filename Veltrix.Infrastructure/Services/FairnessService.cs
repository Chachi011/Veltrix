using Microsoft.EntityFrameworkCore;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Fairness;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Crash;
using Veltrix.Domain.Enums;
using Veltrix.Domain.Progress;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

public sealed class FairnessService : IFairnessService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly AchievementAwarder _awarder;

    public FairnessService(ApplicationDbContext db, ICurrentUserService currentUser, AchievementAwarder awarder)
    {
        _db = db;
        _currentUser = currentUser;
        _awarder = awarder;
    }

    public async Task<FairnessRoundDto> GetRoundAsync(int roundId, CancellationToken ct = default)
    {
        var round = await _db.GameRounds.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roundId, ct)
            ?? throw new NotFoundException($"Round {roundId} was not found.");

        var revealed = round.IsResultRevealed;

        return new FairnessRoundDto
        {
            RoundId = round.Id,
            RoundNumber = round.RoundNumber,
            State = round.State,
            ServerSeedHash = round.ServerSeedHash,
            ServerSeed = revealed ? round.ServerSeed : null,
            CrashPoint = revealed ? round.CrashPoint : null,
            IsVerified = revealed ? ProvablyFair.Verify(round.ServerSeed, round.ServerSeedHash, round.CrashPoint) : null,
            Algorithm = ProvablyFair.AlgorithmDescription
        };
    }

    /// <summary>
    /// The calculation needs no database: anyone can verify any seed, even without an account. A logged-in player who
    /// verifies a REAL round (the seed matches the hash published for a round whose result is revealed) completes the
    /// learning task "Check a round"; a made-up seed completes nothing.
    /// </summary>
    public async Task<VerifyFairnessResult> VerifyAsync(VerifyFairnessRequest request, CancellationToken ct = default)
    {
        var seed = request.ServerSeed.Trim();
        var hash = ProvablyFair.HashSeed(seed);
        var crashPoint = ProvablyFair.CalculateCrashPoint(seed);

        if (_currentUser.UserId is { } userId &&
            await _db.GameRounds.AnyAsync(r => r.ServerSeedHash == hash && r.ServerSeed == seed &&
                (r.State == RoundState.Crashed || r.State == RoundState.Finished || r.State == RoundState.Cancelled), ct))
        {
            await _awarder.TryUnlockAsync(userId, AchievementCodes.VerifyRound, ct);
        }

        return new VerifyFairnessResult
        {
            ComputedHash = hash,
            ComputedCrashPoint = crashPoint,
            HashMatches = string.IsNullOrWhiteSpace(request.ServerSeedHash)
                ? null
                : string.Equals(hash, request.ServerSeedHash.Trim(), StringComparison.OrdinalIgnoreCase),
            CrashPointMatches = request.CrashPoint.HasValue ? request.CrashPoint.Value == crashPoint : null,
            Algorithm = ProvablyFair.AlgorithmDescription
        };
    }
}
