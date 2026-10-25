using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Veltrix.Application.DTOs.Leaderboard;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

/// <summary>
/// Public leaderboard (guests can see it too).
/// "All time" reads the pre-calculated PlayerStats rows (one per player, indexed);
/// "day" and "week" aggregate only the bets of that window.
/// </summary>
public sealed class LeaderboardService : ILeaderboardService
{
    /// <summary>
    /// The page is public and every visitor may refresh it. With a 10-second cache the database answers each
    /// period/type combination at most 6 times a minute, no matter how many people are watching.
    /// </summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _time;

    public LeaderboardService(ApplicationDbContext db, IMemoryCache cache, TimeProvider time)
    {
        _db = db;
        _cache = cache;
        _time = time;
    }

    private sealed record Row(int UserId, decimal Value, int BetsCount);

    public async Task<LeaderboardDto> GetAsync(LeaderboardQuery query, CancellationToken ct = default)
    {
        var key = $"leaderboard:{query.Period}:{query.Type}:{query.Limit}";

        var cached = await _cache.GetOrCreateAsync(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return BuildAsync(query, ct);
        });

        return cached!;
    }

    private async Task<LeaderboardDto> BuildAsync(LeaderboardQuery query, CancellationToken ct)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        DateTime? from = query.Period switch
        {
            LeaderboardPeriod.Day => now.AddDays(-1),
            LeaderboardPeriod.Week => now.AddDays(-7),
            _ => null
        };

        var rows = from is null
            ? await AllTimeAsync(query, ct)
            : await WindowAsync(query, from.Value, ct);

        var ids = rows.Select(r => r.UserId).ToList();
        var players = await _db.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Nickname, u.AvatarUrl, Level = u.Stats != null ? u.Stats.LevelNumber : 1 })
            .ToDictionaryAsync(u => u.Id, ct);

        return new LeaderboardDto
        {
            Period = query.Period,
            Type = query.Type,
            From = from,
            GeneratedAt = now,
            Entries = rows
                .Where(r => players.ContainsKey(r.UserId))
                .Select((r, i) => new LeaderboardEntryDto
                {
                    Rank = i + 1,
                    Nickname = players[r.UserId].Nickname,
                    AvatarUrl = players[r.UserId].AvatarUrl,
                    Level = players[r.UserId].Level,
                    Value = r.Value,
                    BetsCount = r.BetsCount
                })
                .ToList()
        };
    }

    private async Task<List<Row>> AllTimeAsync(LeaderboardQuery query, CancellationToken ct)
    {
        // Blocked accounts are hidden from every public list
        var stats = _db.PlayerStats.AsNoTracking().Where(s => s.TotalBets > 0 && !s.User!.IsBlocked);

        if (query.Type == LeaderboardType.Multiplier)
        {
            var best = await stats
                .Where(s => s.BiggestMultiplier > 0)
                .OrderByDescending(s => s.BiggestMultiplier).ThenBy(s => s.UserId)
                .Take(query.Limit)
                .Select(s => new { s.UserId, Value = s.BiggestMultiplier, s.TotalBets })
                .ToListAsync(ct);
            return best.Select(x => new Row(x.UserId, x.Value, x.TotalBets)).ToList();
        }

        var profit = await stats
            .OrderByDescending(s => s.TotalPayout - s.TotalWagered).ThenBy(s => s.UserId)
            .Take(query.Limit)
            .Select(s => new { s.UserId, Value = s.TotalPayout - s.TotalWagered, s.TotalBets })
            .ToListAsync(ct);
        return profit.Select(x => new Row(x.UserId, x.Value, x.TotalBets)).ToList();
    }

    private async Task<List<Row>> WindowAsync(LeaderboardQuery query, DateTime from, CancellationToken ct)
    {
        var perPlayer = _db.Bets
            .AsNoTracking()
            .Where(b => b.PlacedAt >= from
                        && (b.Status == BetStatus.CashedOut || b.Status == BetStatus.Lost)
                        && !b.User!.IsBlocked)
            .GroupBy(b => b.UserId);

        if (query.Type == LeaderboardType.Multiplier)
        {
            // MAX ignores NULLs, so players who only lost in this window get NULL and are left out
            var best = await perPlayer
                .Select(g => new { UserId = g.Key, Value = g.Max(b => b.CashOutMultiplier), Count = g.Count() })
                .Where(x => x.Value != null)
                .OrderByDescending(x => x.Value).ThenBy(x => x.UserId)
                .Take(query.Limit)
                .ToListAsync(ct);
            return best.Select(x => new Row(x.UserId, x.Value!.Value, x.Count)).ToList();
        }

        var profit = await perPlayer
            .Select(g => new { UserId = g.Key, Value = g.Sum(b => (b.Payout ?? 0) - b.Amount), Count = g.Count() })
            .OrderByDescending(x => x.Value).ThenBy(x => x.UserId)
            .Take(query.Limit)
            .ToListAsync(ct);
        return profit.Select(x => new Row(x.UserId, x.Value, x.Count)).ToList();
    }
}
