using Microsoft.EntityFrameworkCore;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

/// <summary>
/// Calculates one row of the admin charts (one UTC day). Used by DailyStatisticsJob to store finished days,
/// and by AdminStatsService for today and for any day the job has not stored yet.
/// </summary>
internal static class DailyStatisticsCalculator
{
    /// <summary>
    /// A bet placed at 23:59:58 may be cashed out a few seconds after midnight, so a day is only treated
    /// as final if it was calculated at least this long after it ended.
    /// </summary>
    private static readonly TimeSpan SettleGrace = TimeSpan.FromMinutes(5);

    public static bool IsFinal(DailyStatistic row) => row.UpdatedAt >= DayStart(row.Date).AddDays(1).Add(SettleGrace);

    public static DateTime DayStart(DateOnly date) => date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    public static async Task<DailyStatistic> ComputeAsync(ApplicationDbContext db, DateOnly date, DateTime now, CancellationToken ct)
    {
        var start = DayStart(date);
        var end = start.AddDays(1);

        // One aggregate query per table; the database does the counting, not the API.
        // GroupBy(constant) = "one group with everything", so each query returns at most one row.
        var bets = await db.Bets
            .AsNoTracking()
            .Where(b => b.PlacedAt >= start && b.PlacedAt < end && b.Status != BetStatus.Refunded)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Volume = g.Sum(b => b.Amount),
                Payout = g.Sum(b => b.Payout ?? 0),
                Players = g.Select(b => b.UserId).Distinct().Count()
            })
            .SingleOrDefaultAsync(ct);

        var rounds = await db.GameRounds
            .AsNoTracking()
            .Where(r => r.CrashedAt >= start && r.CrashedAt < end)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), AverageCrashPoint = g.Average(r => r.CrashPoint) })
            .SingleOrDefaultAsync(ct);

        var newUsers = await db.Users.CountAsync(u => u.CreatedAt >= start && u.CreatedAt < end, ct);

        return new DailyStatistic
        {
            Date = date,
            ActiveUsers = bets?.Players ?? 0,
            NewUsers = newUsers,
            RoundsPlayed = rounds?.Count ?? 0,
            BetsCount = bets?.Count ?? 0,
            BetsVolume = bets?.Volume ?? 0,
            PayoutVolume = bets?.Payout ?? 0,
            AverageCrashPoint = Math.Round(rounds?.AverageCrashPoint ?? 0, 2),
            UpdatedAt = now
        };
    }

    public static void CopyValues(DailyStatistic from, DailyStatistic to)
    {
        to.ActiveUsers = from.ActiveUsers;
        to.NewUsers = from.NewUsers;
        to.RoundsPlayed = from.RoundsPlayed;
        to.BetsCount = from.BetsCount;
        to.BetsVolume = from.BetsVolume;
        to.PayoutVolume = from.PayoutVolume;
        to.AverageCrashPoint = from.AverageCrashPoint;
        to.UpdatedAt = from.UpdatedAt;
    }
}
