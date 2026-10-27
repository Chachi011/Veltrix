using Microsoft.EntityFrameworkCore;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Stats;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Crash;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

/// <summary>Personal statistics for the profile page charts.</summary>
public sealed class StatsService : IStatsService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _time;

    public StatsService(ApplicationDbContext db, ICurrentUserService currentUser, TimeProvider time)
    {
        _db = db;
        _currentUser = currentUser;
        _time = time;
    }

    private int CurrentUserId => _currentUser.UserId ?? throw new UnauthorizedException("Not logged in.");

    public async Task<MyStatsDto> GetMyStatsAsync(MyStatsQuery query, CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        var now = _time.GetUtcNow().UtcDateTime;

        // Settled bets only: an active bet has no result yet, a refunded one never happened
        var settled = _db.Bets
            .AsNoTracking()
            .Where(b => b.UserId == userId && (b.Status == BetStatus.CashedOut || b.Status == BetStatus.Lost));

        // All totals in ONE aggregate query (SQL COUNT/SUM/AVG/MAX), not by loading every bet
        var totals = await settled
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                CashOuts = g.Count(b => b.Status == BetStatus.CashedOut),
                Wagered = g.Sum(b => b.Amount),
                Payout = g.Sum(b => b.Payout ?? 0),
                AverageCashOut = g.Average(b => b.CashOutMultiplier),
                BiggestMultiplier = g.Max(b => b.CashOutMultiplier),
                BiggestWin = g.Max(b => b.Status == BetStatus.CashedOut ? (b.Payout ?? 0) - b.Amount : (decimal?)null)
            })
            .SingleOrDefaultAsync(ct);

        var firstDay = now.Date.AddDays(-(query.Days - 1));
        var perDay = await settled
            .Where(b => b.PlacedAt >= firstDay)
            .GroupBy(b => b.PlacedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Bets = g.Count(),
                CashOuts = g.Count(b => b.Status == BetStatus.CashedOut),
                Wagered = g.Sum(b => b.Amount),
                Payout = g.Sum(b => b.Payout ?? 0)
            })
            .ToDictionaryAsync(d => DateOnly.FromDateTime(d.Date), ct);

        // Only the multiplier column of the cashed-out bets; grouped into ranges in memory
        var cashOuts = await settled
            .Where(b => b.Status == BetStatus.CashedOut && b.CashOutMultiplier != null)
            .Select(b => b.CashOutMultiplier!.Value)
            .ToListAsync(ct);

        var count = totals?.Count ?? 0;
        var wagered = totals?.Wagered ?? 0;
        var payout = totals?.Payout ?? 0;

        return new MyStatsDto
        {
            TotalBets = count,
            CashOutCount = totals?.CashOuts ?? 0,
            LostCount = count - (totals?.CashOuts ?? 0),
            WinRate = count == 0 ? 0 : Math.Round((totals?.CashOuts ?? 0) * 100m / count, 2),
            TotalWagered = wagered,
            TotalPayout = payout,
            NetProfit = payout - wagered,
            AverageBet = count == 0 ? 0 : Math.Round(wagered / count, 2),
            AverageCashOut = Math.Round(totals?.AverageCashOut ?? 0, 2),
            BiggestMultiplier = totals?.BiggestMultiplier ?? 0,
            BiggestWin = Math.Max(0, totals?.BiggestWin ?? 0),
            Daily = Enumerable.Range(0, query.Days)
                .Select(i => DateOnly.FromDateTime(firstDay.AddDays(i)))
                .Select(date => perDay.TryGetValue(date, out var d)
                    ? new DailyPointDto
                    {
                        Date = date,
                        Bets = d.Bets,
                        CashOuts = d.CashOuts,
                        Wagered = d.Wagered,
                        Payout = d.Payout,
                        Profit = d.Payout - d.Wagered
                    }
                    : new DailyPointDto { Date = date })
                .ToList(),
            CashOutDistribution = BucketMapping.ToDtos(MultiplierBuckets.Distribute(cashOuts), includeExpected: false)
        };
    }
}
