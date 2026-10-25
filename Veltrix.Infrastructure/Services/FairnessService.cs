using Microsoft.EntityFrameworkCore;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.DTOs.Fairness;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Crash;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

public sealed class FairnessService : IFairnessService
{
    private readonly ApplicationDbContext _db;

    public FairnessService(ApplicationDbContext db)
    {
        _db = db;
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

    /// <summary>Pure calculation, no database: anyone can verify any seed, even without an account.</summary>
    public VerifyFairnessResult Verify(VerifyFairnessRequest request)
    {
        var seed = request.ServerSeed.Trim();
        var hash = ProvablyFair.HashSeed(seed);
        var crashPoint = ProvablyFair.CalculateCrashPoint(seed);

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
