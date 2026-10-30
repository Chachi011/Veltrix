using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.DTOs.Rounds;
using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Domain.Progress;
using Veltrix.Infrastructure.Persistence;
using Veltrix.Infrastructure.Services;

namespace Veltrix.Infrastructure.Engine;

public sealed record AutoCashOutCandidate(int BetId, decimal Target);

public sealed record CashOutNotice(int UserId, string Nickname, decimal Multiplier, decimal Payout, decimal Balance);

public sealed record BalanceChange(int UserId, decimal Balance);

public sealed record CancelOutcome(GameRound Round, RoundCancellationDto Cancellation, IReadOnlyList<BalanceChange> Balances);

/// <summary>
/// The crashed round, plus what the engine must push live after the commit: new balances (achievement rewards)
/// and new bell notifications (achievement unlocked, level up).
/// </summary>
public sealed record CrashOutcome(GameRound Round, IReadOnlyList<BalanceChange> Balances, IReadOnlyList<Notification> Notifications);

/// <summary>
/// Every database step of a round's life. The engine decides WHEN; this class decides WHAT is saved.
/// Each public method is one short unit of work (the engine creates a fresh DI scope for each call).
/// </summary>
public sealed class RoundSettlementService
{
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _time;
    private readonly ILogger<RoundSettlementService> _logger;

    public RoundSettlementService(ApplicationDbContext db, TimeProvider time, ILogger<RoundSettlementService> logger)
    {
        _db = db;
        _time = time;
        _logger = logger;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    public async Task<GameRound> CreateRoundAsync(int gameId, CancellationToken ct)
    {
        var lastNumber = await _db.GameRounds
            .Where(r => r.GameId == gameId)
            .MaxAsync(r => (long?)r.RoundNumber, ct) ?? 0;

        // Seed, hash and crash point are all fixed here, BEFORE any bet exists
        var round = GameRound.Create(gameId, lastNumber + 1, Now);
        _db.GameRounds.Add(round);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Round {RoundId} (#{RoundNumber}) created for game {GameId}, hash {Hash}",
            round.Id, round.RoundNumber, gameId, round.ServerSeedHash);
        return round;
    }

    public Task<GameRound> OpenBettingAsync(int roundId, TimeSpan duration, CancellationToken ct) =>
        TransitionAsync(roundId, round => round.OpenBetting(Now, duration), ct);

    public Task<GameRound> CloseBettingAsync(int roundId, CancellationToken ct) =>
        TransitionAsync(roundId, round => round.CloseBetting(Now), ct);

    public Task<GameRound> StartAsync(int roundId, CancellationToken ct) =>
        TransitionAsync(roundId, round => round.Start(Now), ct);

    public Task<GameRound> FinishAsync(int roundId, CancellationToken ct) =>
        TransitionAsync(roundId, round => round.Finish(Now), ct);

    /// <summary>Bets with an auto cash-out target, lowest target first.</summary>
    public async Task<IReadOnlyList<AutoCashOutCandidate>> GetAutoCashOutsAsync(int roundId, CancellationToken ct) =>
        await _db.Bets
            .AsNoTracking()
            .Where(b => b.RoundId == roundId && b.Status == BetStatus.Active && b.AutoCashOutAt != null)
            .OrderBy(b => b.AutoCashOutAt)
            .Select(b => new AutoCashOutCandidate(b.Id, b.AutoCashOutAt!.Value))
            .ToListAsync(ct);

    /// <summary>Pays an auto cash-out at exactly its target multiplier. Null if the bet is no longer active.</summary>
    public Task<CashOutNotice?> AutoCashOutAsync(int betId, decimal multiplier, CancellationToken ct) =>
        ConcurrencyRetry.ExecuteAsync(_db, async () =>
        {
            var bet = await _db.Bets.Include(b => b.User).FirstAsync(b => b.Id == betId, ct);

            // The player may have cashed out manually a moment earlier
            if (bet.Status != BetStatus.Active)
                return null;

            var wallet = await _db.Wallets.FirstAsync(w => w.UserId == bet.UserId, ct);
            var now = Now;
            var payout = bet.CashOutAt(multiplier, now);
            wallet.Credit(payout, TransactionType.Win, $"win:{bet.Id}", now, $"Auto cash-out at {multiplier:0.00}x", bet);

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Bet {BetId} auto cashed out at {Multiplier}x, payout {Payout}, balance {Balance}",
                bet.Id, multiplier, payout, wallet.Balance);
            return (CashOutNotice?)new CashOutNotice(bet.UserId, bet.User!.Nickname, multiplier, payout, wallet.Balance);
        });

    /// <summary>
    /// The round crashes: every bet that is still active is lost, player statistics are updated, and
    /// Play achievements and level-ups are awarded. One transaction, so the round is never "crashed" with some
    /// bets still unsettled, and a reward is never paid twice.
    /// </summary>
    public Task<CrashOutcome> CrashAsync(int roundId, CancellationToken ct) =>
        ConcurrencyRetry.ExecuteAsync(_db, async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            var now = Now;

            // Saving the state change FIRST locks the round row; bets can no longer sneak in
            var round = await _db.GameRounds.FirstAsync(r => r.Id == roundId, ct);
            round.Crash(now);
            await _db.SaveChangesAsync(ct);

            var bets = await _db.Bets.Where(b => b.RoundId == roundId).ToListAsync(ct);
            foreach (var bet in bets.Where(b => b.Status == BetStatus.Active))
                bet.MarkLost();

            var stats = await UpdatePlayerStatsAsync(bets, now, ct);
            var (balances, notifications) = await AwardProgressAsync(bets, stats, now, ct);

            // If a manual cash-out changed one of these bets meanwhile, this throws a concurrency
            // error and the whole step is retried with fresh data
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Round {RoundId} crashed at {CrashPoint}x; {BetCount} bets settled ({Lost} lost), {Rewards} rewards",
                round.Id, round.CrashPoint, bets.Count, bets.Count(b => b.Status == BetStatus.Lost), notifications.Count);
            return new CrashOutcome(round, balances, notifications);
        });

    /// <summary>Cancels a round and gives every active bet its stake back.</summary>
    public Task<CancelOutcome> CancelAsync(int roundId, int? actorUserId, string reason, CancellationToken ct) =>
        ConcurrencyRetry.ExecuteAsync(_db, async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);
            var now = Now;

            var round = await _db.GameRounds.FirstOrDefaultAsync(r => r.Id == roundId, ct)
                ?? throw new NotFoundException($"Round {roundId} was not found.");

            if (round.IsFinal)
                throw new ConflictException($"Round {roundId} is already {round.State}.");

            var previousState = round.State;
            round.Cancel(now, reason); // throws if the round has already crashed
            await _db.SaveChangesAsync(ct); // locks the round row first (see PlaceBet)

            var activeBets = await _db.Bets
                .Where(b => b.RoundId == roundId && b.Status == BetStatus.Active)
                .ToListAsync(ct);

            var userIds = activeBets.Select(b => b.UserId).Distinct().ToList();
            var wallets = await _db.Wallets.Where(w => userIds.Contains(w.UserId)).ToDictionaryAsync(w => w.UserId, ct);

            foreach (var bet in activeBets)
            {
                bet.MarkRefunded();
                wallets[bet.UserId].Credit(bet.Amount, TransactionType.Refund, $"refund:{bet.Id}", now,
                    $"Round #{round.RoundNumber} cancelled", bet);
            }

            var cancellation = new RoundCancellation
            {
                RoundId = round.Id,
                CancelledByUserId = actorUserId,
                Reason = reason,
                RefundedBetsCount = activeBets.Count,
                RefundedTotal = activeBets.Sum(b => b.Amount),
                CancelledAt = now
            };
            _db.RoundCancellations.Add(cancellation);

            _db.AuditLogs.Add(AuditTrail.Entry(actorUserId, AuditActions.RoundCancelled, nameof(GameRound), round.Id.ToString(),
                new { State = previousState.ToString() },
                new { State = round.State.ToString(), reason, cancellation.RefundedBetsCount, cancellation.RefundedTotal },
                now));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogWarning("Round {RoundId} cancelled by {Actor}: {Reason}. Refunded {Count} bets, {Total} VLX",
                round.Id, actorUserId?.ToString() ?? "system", reason, cancellation.RefundedBetsCount, cancellation.RefundedTotal);

            return new CancelOutcome(
                round,
                new RoundCancellationDto
                {
                    RoundId = round.Id,
                    RefundedBetsCount = cancellation.RefundedBetsCount,
                    RefundedTotal = cancellation.RefundedTotal,
                    Reason = reason,
                    CancelledAt = now
                },
                wallets.Values.Select(w => new BalanceChange(w.UserId, w.Balance)).ToList());
        });

    /// <summary>
    /// On startup: rounds left unfinished by a previous crash/restart of the server.
    /// A crashed round only needs finishing; any other unfinished round is cancelled and refunded.
    /// </summary>
    public async Task<IReadOnlyList<CancelOutcome>> RecoverUnfinishedRoundsAsync(CancellationToken ct)
    {
        var unfinished = await _db.GameRounds
            .AsNoTracking()
            .Where(r => r.State != RoundState.Finished && r.State != RoundState.Cancelled)
            .Select(r => new { r.Id, r.State })
            .ToListAsync(ct);

        var outcomes = new List<CancelOutcome>();
        foreach (var round in unfinished)
        {
            _db.ChangeTracker.Clear();

            if (round.State == RoundState.Crashed)
                await FinishAsync(round.Id, ct);
            else
                outcomes.Add(await CancelAsync(round.Id, null, "Server restarted during the round", ct));
        }

        return outcomes;
    }

    private async Task<GameRound> TransitionAsync(int roundId, Action<GameRound> change, CancellationToken ct)
    {
        var round = await _db.GameRounds.FirstAsync(r => r.Id == roundId, ct);
        change(round);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Round {RoundId} -> {State}", round.Id, round.State);
        return round;
    }

    /// <summary>Totals and records of every player in the round (PlayerStats.AddSettledBet, a Domain rule; no XP for bets).</summary>
    private async Task<Dictionary<int, PlayerStats>> UpdatePlayerStatsAsync(IReadOnlyCollection<Bet> bets, DateTime now, CancellationToken ct)
    {
        var userIds = bets.Select(b => b.UserId).Distinct().ToList();
        var stats = await _db.PlayerStats.Where(s => userIds.Contains(s.UserId)).ToDictionaryAsync(s => s.UserId, ct);

        foreach (var bet in bets)
        {
            if (!stats.TryGetValue(bet.UserId, out var s))
            {
                s = new PlayerStats { UserId = bet.UserId, LevelNumber = 1 };
                _db.PlayerStats.Add(s);
                stats[bet.UserId] = s;
            }

            s.AddSettledBet(bet, now);
        }

        return stats;
    }

    /// <summary>
    /// The Play achievements (spec 4.6) of the players of this round: try the game, try cash-out, try auto cash-out.
    /// Rules: AchievementRules in the Domain; what an unlock does (XP, VLX reward with a ledger row, notification):
    /// ProgressRewards. Retired achievements are never awarded. Everything is saved with the crash itself.
    /// </summary>
    private async Task<(List<BalanceChange> Balances, List<Notification> Notifications)> AwardProgressAsync(
        IReadOnlyCollection<Bet> bets, Dictionary<int, PlayerStats> stats, DateTime now, CancellationToken ct)
    {
        var notifications = new List<Notification>();
        var rewarded = new Dictionary<int, Wallet>();
        if (stats.Count == 0)
            return (new List<BalanceChange>(), notifications);

        var userIds = stats.Keys.ToList();
        var achievements = await _db.Achievements.AsNoTracking()
            .Where(a => a.IsActive && AchievementCodes.CompletedBySettlement.Contains(a.Code))
            .OrderBy(a => a.Id)
            .ToListAsync(ct);
        var levels = await _db.Levels.AsNoTracking().ToListAsync(ct);
        var unlocked = (await _db.UserAchievements
                .Where(u => userIds.Contains(u.UserId))
                .Select(u => new { u.UserId, u.AchievementId })
                .ToListAsync(ct))
            .Select(u => (u.UserId, u.AchievementId))
            .ToHashSet();

        foreach (var (userId, s) in stats)
        {
            var betsOfThePlayer = bets.Where(b => b.UserId == userId).ToList();

            foreach (var achievement in achievements)
            {
                if (unlocked.Contains((userId, achievement.Id)) || !AchievementRules.IsEarnedAtSettlement(achievement.Code, s, betsOfThePlayer))
                    continue;

                Wallet? wallet = null;
                if (achievement.VlxReward > 0 && !rewarded.TryGetValue(userId, out wallet))
                {
                    wallet = await _db.Wallets.FirstAsync(w => w.UserId == userId, ct);
                    rewarded[userId] = wallet;
                }

                notifications.Add(ProgressRewards.Unlock(_db, userId, achievement, s, wallet, now));
            }

            if (ProgressRewards.LevelUp(_db, userId, s, levels, now) is { } levelUp)
                notifications.Add(levelUp);
        }

        return (rewarded.Values.Select(w => new BalanceChange(w.UserId, w.Balance)).ToList(), notifications);
    }
}
