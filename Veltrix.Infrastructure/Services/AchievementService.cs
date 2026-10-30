using Microsoft.EntityFrameworkCore;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Achievements;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Enums;
using Veltrix.Domain.Progress;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

/// <summary>
/// Reads the achievements catalog and a player's achievements, and records that the rules were read.
/// Unlocking happens at the round settlement (Play) or in AchievementAwarder (Learn, Safety).
/// </summary>
public sealed class AchievementService : IAchievementService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly AchievementAwarder _awarder;
    private readonly TimeProvider _time;

    public AchievementService(ApplicationDbContext db, ICurrentUserService currentUser, AchievementAwarder awarder, TimeProvider time)
    {
        _db = db;
        _currentUser = currentUser;
        _awarder = awarder;
        _time = time;
    }

    private int CurrentUserId => _currentUser.UserId ?? throw new UnauthorizedException("Not logged in.");

    public async Task<IReadOnlyList<AchievementDto>> GetAllAsync(CancellationToken ct = default)
    {
        var active = await _db.Achievements
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.Id)
            .Select(a => new AchievementDto
            {
                Code = a.Code,
                Title = a.Title,
                Description = a.Description,
                Icon = a.Icon,
                XpReward = a.XpReward,
                VlxReward = a.VlxReward,
                UnlockedBy = a.UnlockedBy.Count,
                Kind = a.Kind
            })
            .ToListAsync(ct);

        // Safety and learning first: the platform prefers them to playing (OrderBy is stable, so the seed order stays inside a kind)
        return active.OrderBy(a => AchievementRules.DisplayOrder(a.Kind)).ToList();
    }

    public async Task<MyAchievementsDto> GetMineAsync(CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        var catalog = await GetAllAsync(ct);
        var stats = await _db.PlayerStats.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);
        var unlocked = await _db.UserAchievements
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.Achievement!.Code, u.UnlockedAt })
            .ToDictionaryAsync(u => u.Code, u => u.UnlockedAt, ct);
        var canPlay = await CanPlayNowAsync(userId, ct);

        var items = new List<MyAchievementDto>();
        foreach (var a in catalog)
        {
            var unlockedAt = unlocked.TryGetValue(a.Code, out var at) ? at : (DateTime?)null;
            if (!AchievementVisibility.IsShown(a.Kind, unlockedAt is not null, canPlay))
                continue;

            var progress = AchievementRules.Progress(a.Code, stats);
            items.Add(new MyAchievementDto
            {
                Code = a.Code,
                Title = a.Title,
                Description = a.Description,
                Icon = a.Icon,
                XpReward = a.XpReward,
                VlxReward = a.VlxReward,
                UnlockedBy = a.UnlockedBy,
                Kind = a.Kind,
                UnlockedAt = unlockedAt,
                // An unlocked achievement shows as complete even if it was earned under older totals
                Progress = unlockedAt is null ? progress?.Current : progress?.Target,
                Target = progress?.Target
            });
        }

        return new MyAchievementsDto { Items = items, PlayTasksHidden = items.Count < catalog.Count };
    }

    public async Task RecordPageReadAsync(string slug, CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        if (!await _db.StaticPages.AnyAsync(p => p.Slug == slug, ct))
            throw new NotFoundException($"Page '{slug}' was not found.");

        if (AchievementRules.CompletedByReading(slug) is { } code)
            await _awarder.TryUnlockAsync(userId, code, ct);
    }

    /// <summary>Self-excluded, or what is left of today's limit is below the smallest bet of any active game: cannot play now.</summary>
    private async Task<bool> CanPlayNowAsync(int userId, CancellationToken ct)
    {
        var limit = await _db.PlayerLimits.AsNoTracking().FirstOrDefaultAsync(l => l.UserId == userId, ct);
        if (limit is null)
            return true;

        var now = _time.GetUtcNow().UtcDateTime;
        var betToday = limit.EffectiveDailyBetLimit(now) is null ? 0 : await _db.Bets.TotalBetTodayAsync(userId, now, ct);
        return limit.CanStillBetToday(betToday, await _db.Games.SmallestActiveMinBetAsync(ct), now);
    }
}
