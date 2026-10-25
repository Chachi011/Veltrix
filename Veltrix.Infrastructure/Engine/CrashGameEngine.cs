using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Realtime;
using Veltrix.Application.DTOs.Rounds;
using Veltrix.Domain.Crash;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Persistence;
using Veltrix.Infrastructure.Services;

namespace Veltrix.Infrastructure.Engine;

public sealed class CrashEngineOptions
{
    public const string SectionName = "CrashEngine";

    /// <summary>Turn the engine off (e.g. for integration tests).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How often MultiplierTick is pushed to clients.</summary>
    public int TickIntervalMs { get; set; } = 100;

    /// <summary>Short pause between BettingClosed and Running ("Get ready...").</summary>
    public int StartDelayMs { get; set; } = 1000;

    /// <summary>How often a game in Maintenance/Disabled is re-checked.</summary>
    public int IdlePollMs { get; set; } = 2000;

    /// <summary>How often the engine looks for newly created games.</summary>
    public int GameScanIntervalSec { get; set; } = 15;
}

/// <summary>
/// The server-side game engine: one endless loop per Crash game, running every round
/// Waiting → BettingOpen → BettingClosed → Running → Crashed → Finished (or Cancelled).
///
/// Browsers never drive the game. They only receive events (SignalR) and send requests
/// (place bet, cash out), which the server checks against its own state and clock.
/// </summary>
public sealed class CrashGameEngine : BackgroundService, IRoundEngineControl
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGameNotifier _notifier;
    private readonly TimeProvider _time;
    private readonly CrashEngineOptions _options;
    private readonly ILogger<CrashGameEngine> _logger;

    private readonly ConcurrentDictionary<int, Task> _gameLoops = new();
    private readonly ConcurrentDictionary<int, LiveRound> _liveRounds = new();

    public CrashGameEngine(
        IServiceScopeFactory scopeFactory,
        IGameNotifier notifier,
        TimeProvider time,
        IOptions<CrashEngineOptions> options,
        ILogger<CrashGameEngine> logger)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _time = time;
        _options = options.Value;
        _logger = logger;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    private sealed record GameInfo(int Id, string Slug, GameStatus Status, int BettingDurationSec, int PauseBetweenRoundsSec);

    /// <summary>Engine-side handle of the round currently being played, used for cancellation.</summary>
    private sealed class LiveRound
    {
        public LiveRound(CancellationTokenSource cts) => Cts = cts;

        public CancellationTokenSource Cts { get; }
        public object Sync { get; } = new();

        /// <summary>Becomes false right before the crash is settled: the result is final from then on.</summary>
        public bool CanBeCancelled { get; set; } = true;

        public int? CancelledBy { get; set; }
        public string? CancelReason { get; set; }

        public TaskCompletionSource<RoundCancellationDto> Cancellation { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Crash engine starting");

        try
        {
            var recovered = await WithSettlementAsync(s => s.RecoverUnfinishedRoundsAsync(stoppingToken));
            foreach (var outcome in recovered)
                await NotifyBalancesAsync(outcome.Balances);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Recovering unfinished rounds failed");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var gameId in await LoadCrashGameIdsAsync(stoppingToken))
                {
                    // Started once per game and left running; the task is kept so shutdown can await it
                    _ = _gameLoops.GetOrAdd(gameId, id => RunGameLoopAsync(id, stoppingToken));
                }

                await Task.Delay(TimeSpan.FromSeconds(_options.GameScanIntervalSec), _time, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Crash engine game scan failed");
                await Task.Delay(TimeSpan.FromSeconds(5), _time, CancellationToken.None);
            }
        }

        await Task.WhenAll(_gameLoops.Values);
        _logger.LogInformation("Crash engine stopped");
    }

    public async Task<RoundCancellationDto> CancelRoundAsync(int roundId, int? actorUserId, string reason, CancellationToken ct = default)
    {
        if (_liveRounds.TryGetValue(roundId, out var live))
        {
            // Same lock as the round loop uses before the crash: either we cancel first,
            // or the crash wins and we answer 409. Never both.
            lock (live.Sync)
            {
                if (!live.CanBeCancelled)
                    throw new ConflictException("The round has already crashed; its result is final.");

                if (!live.Cts.IsCancellationRequested)
                {
                    live.CancelledBy = actorUserId;
                    live.CancelReason = reason;

                    // The game loop notices this, settles the refunds and completes live.Cancellation
                    live.Cts.Cancel();
                }
            }

            return await live.Cancellation.Task.WaitAsync(TimeSpan.FromSeconds(15), ct);
        }

        // Not being played by this engine (e.g. engine disabled): cancel directly in the database
        var outcome = await WithSettlementAsync(s => s.CancelAsync(roundId, actorUserId, reason, ct));
        await NotifyBalancesAsync(outcome.Balances);
        return outcome.Cancellation;
    }

    private async Task RunGameLoopAsync(int gameId, CancellationToken stoppingToken)
    {
        // Yield, so the scan loop can start the other games without waiting for this one
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var game = await LoadGameAsync(gameId, stoppingToken);
                if (game is null)
                {
                    _gameLoops.TryRemove(gameId, out _);
                    return;
                }

                // Maintenance / Disabled: no new rounds, just check again later
                if (game.Status != GameStatus.Active)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(_options.IdlePollMs), _time, stoppingToken);
                    continue;
                }

                await RunRoundAsync(game, stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(game.PauseBetweenRoundsSec), _time, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // One broken round must not kill the game forever: log, wait, start a fresh round
                _logger.LogError(ex, "Round loop of game {GameId} failed; restarting in 5 s", gameId);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), _time, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private async Task RunRoundAsync(GameInfo game, CancellationToken stoppingToken)
    {
        var round = await WithSettlementAsync(s => s.CreateRoundAsync(game.Id, stoppingToken));

        using var roundCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var live = new LiveRound(roundCts);
        _liveRounds[round.Id] = live;

        try
        {
            await _notifier.RoundStateChangedAsync(game.Slug, round.ToStateEvent());

            // 1. Betting
            round = await WithSettlementAsync(s => s.OpenBettingAsync(round.Id, TimeSpan.FromSeconds(game.BettingDurationSec), stoppingToken));
            await _notifier.RoundStateChangedAsync(game.Slug, round.ToStateEvent());
            await DelayUntilAsync(round.BettingEndsAt!.Value, roundCts.Token);

            round = await WithSettlementAsync(s => s.CloseBettingAsync(round.Id, stoppingToken));
            await _notifier.RoundStateChangedAsync(game.Slug, round.ToStateEvent());

            // Betting is closed, so this list cannot change any more
            var autoCashOuts = new Queue<AutoCashOutCandidate>(
                await WithSettlementAsync(s => s.GetAutoCashOutsAsync(round.Id, stoppingToken)));

            await Task.Delay(TimeSpan.FromMilliseconds(_options.StartDelayMs), _time, roundCts.Token);

            // 2. Flight
            round = await WithSettlementAsync(s => s.StartAsync(round.Id, stoppingToken));
            await _notifier.RoundStateChangedAsync(game.Slug, round.ToStateEvent());
            await FlyAsync(game, round, autoCashOuts, roundCts.Token, stoppingToken);

            // 3. Crash: from here the result is final and cannot be cancelled any more
            lock (live.Sync)
            {
                roundCts.Token.ThrowIfCancellationRequested();
                live.CanBeCancelled = false;
            }

            // Auto cash-outs whose target was passed between the last tick and the crash
            while (autoCashOuts.TryDequeue(out var pending))
            {
                if (pending.Target < round.CrashPoint)
                    await AutoCashOutAsync(game.Slug, round.Id, pending, stoppingToken);
            }

            round = await WithSettlementAsync(s => s.CrashAsync(round.Id, stoppingToken));
            await _notifier.RoundCrashedAsync(game.Slug, new RoundCrashedEvent(round.Id, round.CrashPoint, round.ServerSeed));
            await _notifier.RoundStateChangedAsync(game.Slug, round.ToStateEvent());

            round = await WithSettlementAsync(s => s.FinishAsync(round.Id, stoppingToken));
            await _notifier.RoundStateChangedAsync(game.Slug, round.ToStateEvent());
        }
        catch (OperationCanceledException) when (roundCts.IsCancellationRequested && !stoppingToken.IsCancellationRequested)
        {
            // An Operator cancelled this round
            try
            {
                var outcome = await WithSettlementAsync(s =>
                    s.CancelAsync(round.Id, live.CancelledBy, live.CancelReason ?? "Cancelled by operator", stoppingToken));

                await _notifier.RoundStateChangedAsync(game.Slug, outcome.Round.ToStateEvent());
                await NotifyBalancesAsync(outcome.Balances);
                live.Cancellation.TrySetResult(outcome.Cancellation);
            }
            catch (Exception ex)
            {
                live.Cancellation.TrySetException(ex);
                throw;
            }
        }
        finally
        {
            // Close the door before roundCts is disposed, so a late cancel request gets a clean 409
            lock (live.Sync)
                live.CanBeCancelled = false;

            _liveRounds.TryRemove(round.Id, out _);
            live.Cancellation.TrySetException(new ConflictException("The round is already over."));
        }
    }

    /// <summary>The running phase: push the multiplier every tick and pay auto cash-outs when their target is reached.</summary>
    private async Task FlyAsync(
        GameInfo game,
        GameRound round,
        Queue<AutoCashOutCandidate> autoCashOuts,
        CancellationToken roundToken,
        CancellationToken stoppingToken)
    {
        var startedAt = round.StartedAt!.Value;
        var crashesAt = round.CrashesAt!.Value;
        var tick = TimeSpan.FromMilliseconds(_options.TickIntervalMs);

        while (true)
        {
            var now = Now;
            var multiplier = MultiplierCurve.At(now - startedAt);

            if (multiplier >= round.CrashPoint)
                return;

            while (autoCashOuts.TryPeek(out var next) && next.Target <= multiplier)
            {
                autoCashOuts.Dequeue();
                await AutoCashOutAsync(game.Slug, round.Id, next, stoppingToken);
            }

            await _notifier.MultiplierTickAsync(game.Slug,
                new MultiplierTickEvent(round.Id, multiplier, (long)(now - startedAt).TotalMilliseconds));

            // Wake up for the next tick, or exactly at the crash moment if that comes first
            var untilCrash = crashesAt - Now;
            var delay = untilCrash < tick ? untilCrash : tick;
            await Task.Delay(delay > TimeSpan.FromMilliseconds(1) ? delay : TimeSpan.FromMilliseconds(1), _time, roundToken);
        }
    }

    private async Task AutoCashOutAsync(string slug, int roundId, AutoCashOutCandidate candidate, CancellationToken ct)
    {
        try
        {
            var notice = await WithSettlementAsync(s => s.AutoCashOutAsync(candidate.BetId, candidate.Target, ct));
            if (notice is null)
                return;

            await _notifier.PlayerCashedOutAsync(slug, new PlayerCashedOutEvent(roundId, notice.Nickname, notice.Multiplier, notice.Payout));
            await _notifier.BalanceUpdatedAsync(notice.UserId, notice.Balance);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One failed payout must not stop the round for everyone; the bet stays Active and is settled at the crash
            _logger.LogError(ex, "Auto cash-out of bet {BetId} failed", candidate.BetId);
        }
    }

    private async Task NotifyBalancesAsync(IEnumerable<BalanceChange> balances)
    {
        foreach (var change in balances)
            await _notifier.BalanceUpdatedAsync(change.UserId, change.Balance);
    }

    private async Task DelayUntilAsync(DateTime moment, CancellationToken ct)
    {
        var wait = moment - Now;
        if (wait > TimeSpan.Zero)
            await Task.Delay(wait, _time, ct);
    }

    /// <summary>Each DB step gets its own short-lived scope (and DbContext), like a web request does.</summary>
    private async Task<T> WithSettlementAsync<T>(Func<RoundSettlementService, Task<T>> action)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<RoundSettlementService>());
    }

    private async Task<List<int>> LoadCrashGameIdsAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Games.Where(g => g.Type == GameType.Crash).Select(g => g.Id).ToListAsync(ct);
    }

    private async Task<GameInfo?> LoadGameAsync(int gameId, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Games
            .AsNoTracking()
            .Where(g => g.Id == gameId)
            .Select(g => new GameInfo(g.Id, g.Slug, g.Status, g.BettingDurationSec, g.PauseBetweenRoundsSec))
            .FirstOrDefaultAsync(ct);
    }
}
