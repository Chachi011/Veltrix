using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Domain.Common;
using Veltrix.Domain.Crash;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Engine;
using Veltrix.Infrastructure.Services;

namespace Veltrix.Infrastructure.Persistence;

/// <summary>
/// DEVELOPMENT ONLY (Database:SeedDemoData): 15 demo players and 30 days of play, so the leaderboard, the charts and the
/// admin dashboard are not empty in a demo. Nothing goes around the game rules; every step uses the same code as a real round:
/// - each round gets a real random seed, hash and crash point (GameRound.Create), so "verify this round" works for it;
/// - a bet is checked by Bet.Place and takes the stake from the wallet with a ledger row (like BetService);
/// - cash-outs happen BELOW the crash point: automatically (RoundSettlementService.AutoCashOutAsync, the engine's code)
///   or "by hand" with Bet.CashOut at the server multiplier of that moment (like BetService);
/// - the crash settles the remaining bets as lost and updates PlayerStats (RoundSettlementService.CrashAsync).
/// The only difference from a real game is a clock set to moments in the past. Runs once: skipped when the demo players exist.
/// </summary>
public sealed class DemoDataSeeder
{
    public const string EmailDomain = "demo.veltrix.dev";
    public const string Password = "Player123!";
    public const int HistoryDays = 30;

    /// <summary>
    /// How each demo player plays: how often they join a round, their stake range, the multiplier they aim for
    /// and how often they use auto cash-out. Different styles give the leaderboard and the charts some variety.
    /// </summary>
    private sealed record DemoPlayer(
        string Nickname, int JoinedDaysAgo, double Activity,
        decimal MinStake, decimal MaxStake, decimal MinTarget, decimal MaxTarget, double AutoShare);

    private static readonly DemoPlayer[] Profiles =
    {
        new("nova", 34, 0.75, 10, 50, 1.30m, 2.20m, 0.7),
        new("orbit_max", 33, 0.60, 20, 100, 1.50m, 3.00m, 0.5),
        new("comet", 33, 0.50, 5, 25, 2.00m, 6.00m, 0.3),
        new("lunar_lisa", 32, 0.55, 10, 40, 1.20m, 1.60m, 0.9),
        new("rocketeer", 31, 0.45, 25, 150, 1.80m, 4.00m, 0.4),
        new("stellar", 31, 0.50, 5, 20, 1.40m, 2.50m, 0.6),
        new("quasar_q", 30, 0.35, 50, 200, 1.50m, 2.00m, 0.8),
        new("nebula", 29, 0.40, 2, 15, 3.00m, 12.00m, 0.2),
        new("zenith", 27, 0.50, 10, 60, 1.25m, 1.80m, 0.7),
        new("apollo_11", 24, 0.40, 15, 80, 1.60m, 3.50m, 0.5),
        new("vega", 20, 0.45, 5, 30, 1.30m, 2.00m, 0.6),
        new("cosmo_kid", 15, 0.35, 1, 10, 2.00m, 8.00m, 0.3),
        new("pulsar", 10, 0.50, 20, 90, 1.40m, 2.80m, 0.5),
        new("astra", 6, 0.55, 10, 50, 1.20m, 2.20m, 0.8),
        new("meteor_mia", 3, 0.60, 5, 40, 1.50m, 5.00m, 0.4)
    };

    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;

    // Fixed seed: the same players make the same choices on every fresh database (crash points stay truly random)
    private readonly Random _random = new(20261027);

    public DemoDataSeeder(ApplicationDbContext db, IPasswordHasher passwordHasher, TimeProvider time, ILogger logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _time = time;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.Users.AnyAsync(u => u.Email.EndsWith("@" + EmailDomain), ct))
            return;

        var game = await _db.Games.AsNoTracking().OrderBy(g => g.Id).FirstOrDefaultAsync(g => g.Type == GameType.Crash, ct);
        if (game is null)
            return;

        _logger.LogInformation("Seeding demo data: {Players} players and {Days} days of rounds (one time, about half a minute)...",
            Profiles.Length, HistoryDays);

        var now = _time.GetUtcNow().UtcDateTime;
        var players = await CreatePlayersAsync(now, ct);
        if (players.Count == 0)
            return;

        // The engine's own settlement code, driven by a clock that we set to each moment of the past
        var clock = new PastClock(now);
        var settlement = new RoundSettlementService(_db, clock, NullLogger<RoundSettlementService>.Instance);
        var dailyBonus = await SettingsReader.GetDecimalAsync(_db, SettingKeys.DailyBonusAmount, SettingDefaults.DailyBonusAmount, ct);

        var rounds = 0;
        var bets = 0;
        var nextFree = DateTime.MinValue;

        for (var day = now.Date.AddDays(-HistoryDays); day <= now.Date; day = day.AddDays(1))
        {
            var isToday = day == now.Date;

            // Past days: rounds between 09:00 and 23:00. Today: the last 3 hours, ending 10 minutes ago,
            // so even a very long flight is over before "now" (nothing is ever dated in the future).
            var windowStart = isToday ? Later(day.AddMinutes(1), now.AddHours(-3)) : day.AddHours(9);
            var windowEnd = isToday ? now.AddMinutes(-10) : day.AddHours(23);
            if (windowEnd <= windowStart)
                continue;

            var count = isToday ? 14 : _random.Next(5, 10);
            var plannedStarts = Enumerable.Range(0, count)
                .Select(_ => windowStart + (windowEnd - windowStart) * _random.NextDouble())
                .Order()
                .ToList();

            foreach (var planned in plannedStarts)
            {
                // Rounds never overlap: the next one starts after the previous one has finished
                var start = Later(planned, nextFree);
                if (start > windowEnd)
                    break;

                var (end, betCount) = await PlayRoundAsync(game, players, settlement, clock, start, dailyBonus, ct);
                nextFree = end.AddSeconds(_random.Next(5, 60));
                rounds++;
                bets += betCount;
            }
        }

        var days = await RecalculateDailyStatisticsAsync(players.Min(p => p.User.CreatedAt), now, ct);

        _logger.LogInformation("Demo data ready: {Players} players, {Rounds} rounds, {Bets} bets, {Days} days of statistics. " +
                               "Demo players log in with <nickname>@{Domain} / {Password}",
            players.Count, rounds, bets, days, EmailDomain, Password);
    }

    private async Task<List<(DemoPlayer Profile, User User)>> CreatePlayersAsync(DateTime now, CancellationToken ct)
    {
        var startingBalance = await SettingsReader.GetDecimalAsync(_db, SettingKeys.StartingBalance, SettingDefaults.StartingBalance, ct);
        var passwordHash = _passwordHasher.Hash(Password); // one BCrypt hash for all of them: hashing is slow on purpose

        var players = new List<(DemoPlayer, User)>();
        foreach (var profile in Profiles)
        {
            // A real player may already use this nickname; never touch their account
            if (await _db.Users.AnyAsync(u => u.Nickname == profile.Nickname, ct))
                continue;

            var joined = now.Date.AddDays(-profile.JoinedDaysAgo).AddHours(_random.Next(8, 22)).AddMinutes(_random.Next(60));
            var born = new DateOnly(1980 + _random.Next(20), 1 + _random.Next(12), 1 + _random.Next(28));
            var user = User.Create($"{profile.Nickname}@{EmailDomain}", profile.Nickname, passwordHash, born,
                UserRole.Player, startingBalance, joined);

            _db.Users.Add(user);
            players.Add((profile, user));
        }

        await _db.SaveChangesAsync(ct);
        return players;
    }

    /// <summary>One full round: create → betting → bets → closed → running → cash-outs → crash → finished.</summary>
    /// <returns>When the round finished, and how many bets it had.</returns>
    private async Task<(DateTime End, int Bets)> PlayRoundAsync(
        Game game,
        List<(DemoPlayer Profile, User User)> players,
        RoundSettlementService settlement,
        PastClock clock,
        DateTime start,
        decimal dailyBonus,
        CancellationToken ct)
    {
        clock.Now = start;
        var round = await settlement.CreateRoundAsync(game.Id, ct); // seed, hash and crash point fixed here

        clock.Now = start.AddSeconds(1);
        await settlement.OpenBettingAsync(round.Id, TimeSpan.FromSeconds(10), ct);

        var placed = new List<(Bet Bet, Wallet Wallet, decimal Target, bool Auto)>();
        foreach (var (profile, user) in players)
        {
            if (user.CreatedAt >= start || _random.NextDouble() > profile.Activity)
                continue;

            var placedAt = start.AddSeconds(1 + _random.NextDouble() * 8);
            var wallet = user.Wallet!;
            var stake = Math.Round(Between(profile.MinStake, profile.MaxStake), 0);

            // Out of points: the daily bonus is the only way back in (virtual points, no deposits)
            if (wallet.Balance < stake && wallet.CanClaimDailyBonus(placedAt))
                wallet.ClaimDailyBonus(dailyBonus, placedAt);

            stake = Math.Min(stake, Math.Floor(wallet.Balance));
            if (stake < game.MinBet)
                continue;

            var target = Math.Round(Between(profile.MinTarget, profile.MaxTarget), 2);
            var auto = _random.NextDouble() < profile.AutoShare;

            var bet = Bet.Place(round, game, user.Id, stake, 1, auto ? target : null, placedAt);
            wallet.Debit(stake, TransactionType.Bet, $"bet:{round.Id}:{user.Id}:1", placedAt, $"Bet on round #{round.RoundNumber}", bet);
            _db.Bets.Add(bet);
            placed.Add((bet, wallet, target, auto));
        }

        await _db.SaveChangesAsync(ct);

        clock.Now = start.AddSeconds(11);
        await settlement.CloseBettingAsync(round.Id, ct);

        var startedAt = start.AddSeconds(12);
        clock.Now = startedAt;
        await settlement.StartAsync(round.Id, ct);

        // Cash-outs in the order they happen; only targets below the crash point can be reached
        foreach (var (bet, wallet, target, auto) in placed.Where(p => p.Target < round.CrashPoint).OrderBy(p => p.Target))
        {
            if (auto)
            {
                clock.Now = startedAt + MultiplierCurve.TimeToReach(target);
                await settlement.AutoCashOutAsync(bet.Id, target, ct);
                continue;
            }

            // A person clicks a moment after their target, and gets the server multiplier of THAT moment
            var clickedAt = startedAt + MultiplierCurve.TimeToReach(target) + TimeSpan.FromMilliseconds(_random.Next(50, 400));
            if (round.MultiplierAt(clickedAt) >= round.CrashPoint)
                continue; // too late: the bet is lost at the crash

            var payout = bet.CashOut(round, clickedAt);
            wallet.Credit(payout, TransactionType.Win, $"win:{bet.Id}", clickedAt,
                $"Cash-out at {bet.CashOutMultiplier:0.00}x on round #{round.RoundNumber}", bet);
            await _db.SaveChangesAsync(ct);
        }

        clock.Now = round.CrashesAt!.Value;
        await settlement.CrashAsync(round.Id, ct); // remaining bets lost, PlayerStats updated

        var end = clock.Now.AddSeconds(3);
        clock.Now = end;
        await settlement.FinishAsync(round.Id, ct);

        return (end, placed.Count);
    }

    /// <summary>
    /// The statistics job never recalculates a day it already stored as final, so the days that now contain demo bets
    /// are recalculated here (today is always calculated live by the admin dashboard).
    /// </summary>
    private async Task<int> RecalculateDailyStatisticsAsync(DateTime firstJoined, DateTime now, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(now);
        var first = DateOnly.FromDateTime(firstJoined);
        var existing = await _db.DailyStatistics.Where(d => d.Date >= first && d.Date < today).ToDictionaryAsync(d => d.Date, ct);

        var days = 0;
        for (var date = first; date < today; date = date.AddDays(1))
        {
            var fresh = await DailyStatisticsCalculator.ComputeAsync(_db, date, now, ct);
            if (existing.TryGetValue(date, out var row))
                DailyStatisticsCalculator.CopyValues(fresh, row);
            else
                _db.DailyStatistics.Add(fresh);
            days++;
        }

        await _db.SaveChangesAsync(ct);
        return days;
    }

    private decimal Between(decimal min, decimal max) => min + (max - min) * (decimal)_random.NextDouble();

    private static DateTime Later(DateTime a, DateTime b) => a > b ? a : b;

    /// <summary>A clock that shows whatever moment the seeder sets.</summary>
    private sealed class PastClock : TimeProvider
    {
        public PastClock(DateTime now) => Now = now;

        public DateTime Now { get; set; }

        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Now, DateTimeKind.Utc));
    }
}
