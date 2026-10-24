using Microsoft.EntityFrameworkCore;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Rounds;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

public sealed class RoundService : IRoundService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IRoundEngineControl _engine;
    private readonly TimeProvider _time;

    public RoundService(ApplicationDbContext db, ICurrentUserService currentUser, IRoundEngineControl engine, TimeProvider time)
    {
        _db = db;
        _currentUser = currentUser;
        _engine = engine;
        _time = time;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    public async Task<RoundDto> GetCurrentAsync(string gameSlug, CancellationToken ct = default)
    {
        var round = await _db.GameRounds
            .AsNoTracking()
            .Where(r => r.Game!.Slug == gameSlug)
            .OrderByDescending(r => r.RoundNumber)
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"No rounds yet for game '{gameSlug}'.");

        return round.ToDto(gameSlug, Now);
    }

    public async Task<PagedResult<RoundDto>> GetHistoryAsync(string gameSlug, RoundHistoryQuery query, CancellationToken ct = default)
    {
        if (!await _db.Games.AnyAsync(g => g.Slug == gameSlug, ct))
            throw new NotFoundException($"Game '{gameSlug}' was not found.");

        // Only rounds that are over: their seed and crash point may be shown
        var rounds = _db.GameRounds
            .AsNoTracking()
            .Where(r => r.Game!.Slug == gameSlug && (r.State == RoundState.Finished || r.State == RoundState.Cancelled));

        var total = await rounds.CountAsync(ct);
        var page = await rounds
            .OrderByDescending(r => r.RoundNumber)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        var now = Now;
        return new PagedResult<RoundDto>
        {
            Items = page.Select(r => r.ToDto(gameSlug, now)).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<RoundDto> GetByIdAsync(int roundId, CancellationToken ct = default)
    {
        var result = await _db.GameRounds
            .AsNoTracking()
            .Where(r => r.Id == roundId)
            .Select(r => new { Round = r, r.Game!.Slug })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"Round {roundId} was not found.");

        return result.Round.ToDto(result.Slug, Now);
    }

    public async Task<IReadOnlyList<RoundBetDto>> GetBetsAsync(int roundId, CancellationToken ct = default)
    {
        if (!await _db.GameRounds.AnyAsync(r => r.Id == roundId, ct))
            throw new NotFoundException($"Round {roundId} was not found.");

        return await _db.Bets
            .AsNoTracking()
            .Where(b => b.RoundId == roundId)
            .OrderByDescending(b => b.Amount).ThenBy(b => b.PlacedAt)
            .Select(b => new RoundBetDto
            {
                BetId = b.Id,
                Nickname = b.User!.Nickname,
                AvatarUrl = b.User.AvatarUrl,
                Amount = b.Amount,
                Slot = b.Slot,
                Status = b.Status,
                CashOutMultiplier = b.CashOutMultiplier,
                Payout = b.Payout,
                PlacedAt = b.PlacedAt
            })
            .ToListAsync(ct);
    }

    public Task<RoundCancellationDto> CancelAsync(int roundId, CancelRoundRequest request, CancellationToken ct = default) =>
        _engine.CancelRoundAsync(roundId, _currentUser.UserId, request.Reason.Trim(), ct);
}
