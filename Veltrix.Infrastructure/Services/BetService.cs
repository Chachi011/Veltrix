using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Bets;
using Veltrix.Application.DTOs.Realtime;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

public sealed class BetService : IBetService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameNotifier _notifier;
    private readonly TimeProvider _time;
    private readonly ILogger<BetService> _logger;

    public BetService(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IGameNotifier notifier,
        TimeProvider time,
        ILogger<BetService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
        _time = time;
        _logger = logger;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    private int CurrentUserId => _currentUser.UserId ?? throw new UnauthorizedException("Not logged in.");

    public async Task<BetResultDto> PlaceBetAsync(PlaceBetRequest request, CancellationToken ct = default)
    {
        var userId = CurrentUserId;

        var game = await _db.Games.AsNoTracking().FirstOrDefaultAsync(g => g.Slug == request.GameSlug, ct)
            ?? throw new NotFoundException($"Game '{request.GameSlug}' was not found.");

        if (game.Status != GameStatus.Active)
            throw new BadRequestException($"'{game.Name}' is not available right now ({game.Status}).");

        var player = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Nickname, u.IsBlocked, u.Limit })
            .FirstOrDefaultAsync(ct)
            ?? throw new UnauthorizedException("User not found.");

        // The JWT stays valid for up to 15 minutes, so a block must also be checked here
        if (player.IsBlocked)
            throw new ForbiddenException("Your account is blocked.");

        await EnforcePlayerLimitsAsync(userId, player.Limit, request.Amount, ct);

        var (bet, round, balance) = await ConcurrencyRetry.ExecuteAsync(_db, async () =>
        {
            // REPEATABLE READ keeps a shared lock on the round row until we commit, so the engine
            // cannot close or cancel the round between "is betting open?" and "insert the bet".
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
            var now = Now;

            var currentRound = await _db.GameRounds
                .Where(r => r.GameId == game.Id)
                .OrderByDescending(r => r.RoundNumber)
                .FirstOrDefaultAsync(ct)
                ?? throw new BadRequestException("There is no round to bet on yet.");

            if (await _db.Bets.AnyAsync(b => b.RoundId == currentRound.Id && b.UserId == userId && b.Slot == request.Slot, ct))
                throw new ConflictException($"You already have a bet in slot {request.Slot} of this round.");

            // Domain rules: betting open, amount within the game's min/max, valid slot and auto cash-out
            var newBet = Bet.Place(currentRound, game, userId, request.Amount, request.Slot, request.AutoCashOutAt, now);

            var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId, ct);
            wallet.Debit(newBet.Amount, TransactionType.Bet, $"bet:{currentRound.Id}:{userId}:{newBet.Slot}", now,
                $"Bet on round #{currentRound.RoundNumber}", newBet);

            _db.Bets.Add(newBet);

            // Bet + ledger row + new balance: one SaveChanges = one atomic write
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return (newBet, currentRound, wallet.Balance);
        });

        _logger.LogInformation("User {UserId} bet {Amount} on round {RoundId} (slot {Slot}, auto {Auto}); balance {Balance}",
            userId, bet.Amount, round.Id, bet.Slot, bet.AutoCashOutAt, balance);

        await _notifier.BetPlacedAsync(game.Slug, new BetPlacedEvent(round.Id, player.Nickname, bet.Amount));
        await _notifier.BalanceUpdatedAsync(userId, balance);

        return new BetResultDto { Bet = ToDto(bet, round, game.Slug), Balance = balance };
    }

    public async Task<BetResultDto> CashOutAsync(int betId, CancellationToken ct = default)
    {
        var userId = CurrentUserId;

        // The moment the request arrived decides the multiplier, even if we have to retry
        var requestedAt = Now;

        var (bet, round, slug, nickname, balance) = await ConcurrencyRetry.ExecuteAsync(_db, async () =>
        {
            var existing = await _db.Bets
                .Include(b => b.Round!).ThenInclude(r => r.Game)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == betId && b.UserId == userId, ct)
                ?? throw new NotFoundException($"Bet {betId} was not found.");

            // Uses the SERVER's multiplier at requestedAt and refuses if the round has already crashed
            var payout = existing.CashOut(existing.Round!, requestedAt);

            var wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId, ct);
            wallet.Credit(payout, TransactionType.Win, $"win:{existing.Id}", requestedAt,
                $"Cash-out at {existing.CashOutMultiplier:0.00}x on round #{existing.Round!.RoundNumber}", existing);

            // Fails with a concurrency error if the crash settlement already marked the bet as Lost
            await _db.SaveChangesAsync(ct);

            return (existing, existing.Round!, existing.Round!.Game!.Slug, existing.User!.Nickname, wallet.Balance);
        });

        _logger.LogInformation("User {UserId} cashed out bet {BetId} at {Multiplier}x, payout {Payout}; balance {Balance}",
            userId, bet.Id, bet.CashOutMultiplier, bet.Payout, balance);

        await _notifier.PlayerCashedOutAsync(slug, new PlayerCashedOutEvent(round.Id, nickname, bet.CashOutMultiplier!.Value, bet.Payout!.Value));
        await _notifier.BalanceUpdatedAsync(userId, balance);

        return new BetResultDto { Bet = ToDto(bet, round, slug), Balance = balance };
    }

    public async Task<PagedResult<BetDto>> GetMyBetsAsync(MyBetsQuery query, CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        var bets = _db.Bets.AsNoTracking().Where(b => b.UserId == userId);

        if (query.From.HasValue)
            bets = bets.Where(b => b.PlacedAt >= query.From.Value);
        if (query.To.HasValue)
            bets = bets.Where(b => b.PlacedAt <= query.To.Value);
        if (query.Status.HasValue)
            bets = bets.Where(b => b.Status == query.Status.Value);

        var total = await bets.CountAsync(ct);
        var items = await bets
            .OrderByDescending(b => b.PlacedAt).ThenByDescending(b => b.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .Select(b => new BetDto
            {
                Id = b.Id,
                RoundId = b.RoundId,
                RoundNumber = b.Round!.RoundNumber,
                GameSlug = b.Round.Game!.Slug,
                Amount = b.Amount,
                Slot = b.Slot,
                AutoCashOutAt = b.AutoCashOutAt,
                Status = b.Status,
                CashOutMultiplier = b.CashOutMultiplier,
                Payout = b.Payout,
                // Never reveal the crash point of a round that is still being played
                CrashPoint = b.Round.State == RoundState.Crashed || b.Round.State == RoundState.Finished || b.Round.State == RoundState.Cancelled
                    ? b.Round.CrashPoint
                    : null,
                PlacedAt = b.PlacedAt,
                CashedOutAt = b.CashedOutAt
            })
            .ToListAsync(ct);

        return new PagedResult<BetDto> { Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = total };
    }

    /// <summary>Self-exclusion and the daily bet limit (responsible play tools).</summary>
    private async Task EnforcePlayerLimitsAsync(int userId, PlayerLimit? limit, decimal amount, CancellationToken ct)
    {
        if (limit is null)
            return;

        var now = Now;
        if (limit.SelfExcludedUntil > now)
            throw new ForbiddenException($"You are self-excluded until {limit.SelfExcludedUntil:yyyy-MM-dd HH:mm} UTC.");

        if (limit.DailyBetLimit is { } dailyLimit)
        {
            var startOfDay = now.Date;
            var betToday = await _db.Bets
                .Where(b => b.UserId == userId && b.PlacedAt >= startOfDay && b.Status != BetStatus.Refunded)
                .SumAsync(b => (decimal?)b.Amount, ct) ?? 0;

            if (betToday + amount > dailyLimit)
                throw new BadRequestException(
                    $"Daily bet limit reached: {betToday:0.00} of {dailyLimit:0.00} VLX used today.");
        }
    }

    private static BetDto ToDto(Bet bet, GameRound round, string slug) => new()
    {
        Id = bet.Id,
        RoundId = round.Id,
        RoundNumber = round.RoundNumber,
        GameSlug = slug,
        Amount = bet.Amount,
        Slot = bet.Slot,
        AutoCashOutAt = bet.AutoCashOutAt,
        Status = bet.Status,
        CashOutMultiplier = bet.CashOutMultiplier,
        Payout = bet.Payout,
        CrashPoint = round.IsResultRevealed ? round.CrashPoint : null,
        PlacedAt = bet.PlacedAt,
        CashedOutAt = bet.CashedOutAt
    };
}
