using Microsoft.EntityFrameworkCore;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Stats;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Crash;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

/// <summary>Admin dashboard numbers and charts.</summary>
public sealed class AdminStatsService : IAdminStatsService
{
    /// <summary>The crash point chart uses the latest rounds; enough for the shape to be visible.</summary>
    private const int DistributionSample = 1000;

    private readonly ApplicationDbContext _db;
    private readonly IOnlineCounter _online;
    private readonly TimeProvider _time;

    public AdminStatsService(ApplicationDbContext db, IOnlineCounter online, TimeProvider time)
    {
        _db = db;
        _online = online;
        _time = time;
    }

    public async Task<AdminOverviewDto> GetOverviewAsync(CancellationToken ct = default)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var today = await DailyStatisticsCalculator.ComputeAsync(_db, DateOnly.FromDateTime(now), now, ct);

        var users = await _db.Users
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Players = g.Count(u => u.Role == UserRole.Player),
                Blocked = g.Count(u => u.IsBlocked)
            })
            .SingleOrDefaultAsync(ct);

        var circulation = await _db.Wallets.SumAsync(w => (decimal?)w.Balance, ct) ?? 0;

        var crashPoints = await _db.GameRounds
            .AsNoTracking()
            .Where(r => r.State == RoundState.Crashed || r.State == RoundState.Finished)
            .OrderByDescending(r => r.Id)
            .Take(DistributionSample)
            .Select(r => r.CrashPoint)
            .ToListAsync(ct);

        return new AdminOverviewDto
        {
            TotalUsers = users?.Total ?? 0,
            PlayersCount = users?.Players ?? 0,
            BlockedUsers = users?.Blocked ?? 0,
            NewUsersToday = today.NewUsers,
            OnlineNow = _online.OnlineConnections,
            ActivePlayersToday = today.ActiveUsers,
            RoundsToday = today.RoundsPlayed,
            BetsToday = today.BetsCount,
            BetsVolumeToday = today.BetsVolume,
            PayoutToday = today.PayoutVolume,
            HouseResultToday = today.BetsVolume - today.PayoutVolume,
            VlxInCirculation = circulation,
            RoundsSampled = crashPoints.Count,
            AverageCrashPoint = crashPoints.Count == 0 ? 0 : Math.Round(crashPoints.Average(), 2),
            CrashPointDistribution = BucketMapping.ToDtos(MultiplierBuckets.Distribute(crashPoints), includeExpected: true)
        };
    }

    public async Task<IReadOnlyList<DailyStatisticDto>> GetDailyAsync(DailyStatsQuery query, CancellationToken ct = default)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var to = query.To is { } requestedTo && requestedTo < today ? requestedTo : today;
        var from = query.From ?? to.AddDays(-(DailyStatsQuery.DefaultDays - 1));
        if (from > to)
            return Array.Empty<DailyStatisticDto>();

        // Finished days were stored by DailyStatisticsJob: reading 30 small rows instead of scanning every bet
        var stored = await _db.DailyStatistics
            .AsNoTracking()
            .Where(d => d.Date >= from && d.Date <= to)
            .ToDictionaryAsync(d => d.Date, ct);

        // Nothing can have happened before the first account existed
        var firstUserAt = await _db.Users.MinAsync(u => (DateTime?)u.CreatedAt, ct);
        var firstDay = firstUserAt is { } first ? DateOnly.FromDateTime(first) : today;

        var result = new List<DailyStatisticDto>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            if (date < firstDay)
            {
                result.Add(new DailyStatisticDto { Date = date });
                continue;
            }

            // Today is still changing, and a day the job has not stored (or stored too early) is calculated now,
            // so the chart is never wrong, only slower in that rare case
            var row = stored.TryGetValue(date, out var saved) && DailyStatisticsCalculator.IsFinal(saved)
                ? saved
                : await DailyStatisticsCalculator.ComputeAsync(_db, date, now, ct);

            result.Add(ToDto(row));
        }

        return result;
    }

    private static DailyStatisticDto ToDto(DailyStatistic row) => new()
    {
        Date = row.Date,
        ActiveUsers = row.ActiveUsers,
        NewUsers = row.NewUsers,
        RoundsPlayed = row.RoundsPlayed,
        BetsCount = row.BetsCount,
        BetsVolume = row.BetsVolume,
        PayoutVolume = row.PayoutVolume,
        AverageCrashPoint = row.AverageCrashPoint
    };
}
