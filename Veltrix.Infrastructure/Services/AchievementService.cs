using Microsoft.EntityFrameworkCore;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Achievements;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Progress;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

/// <summary>Reads the achievements catalog and a player's achievements. Unlocking happens at the round settlement.</summary>
public sealed class AchievementService : IAchievementService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AchievementService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private int CurrentUserId => _currentUser.UserId ?? throw new UnauthorizedException("Not logged in.");

    public async Task<IReadOnlyList<AchievementDto>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Achievements
            .AsNoTracking()
            .OrderBy(a => a.Id)
            .Select(a => new AchievementDto
            {
                Code = a.Code,
                Title = a.Title,
                Description = a.Description,
                Icon = a.Icon,
                XpReward = a.XpReward,
                VlxReward = a.VlxReward,
                UnlockedBy = a.UnlockedBy.Count
            })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<MyAchievementDto>> GetMineAsync(CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        var catalog = await GetAllAsync(ct);
        var stats = await _db.PlayerStats.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);
        var unlocked = await _db.UserAchievements
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.Achievement!.Code, u.UnlockedAt })
            .ToDictionaryAsync(u => u.Code, u => u.UnlockedAt, ct);

        return catalog
            .Select(a =>
            {
                var progress = AchievementRules.Progress(a.Code, stats);
                var unlockedAt = unlocked.TryGetValue(a.Code, out var at) ? at : (DateTime?)null;
                return new MyAchievementDto
                {
                    Code = a.Code,
                    Title = a.Title,
                    Description = a.Description,
                    Icon = a.Icon,
                    XpReward = a.XpReward,
                    VlxReward = a.VlxReward,
                    UnlockedBy = a.UnlockedBy,
                    UnlockedAt = unlockedAt,
                    // An unlocked achievement shows as complete even if it was earned under older totals
                    Progress = unlockedAt is null ? progress?.Current : progress?.Target,
                    Target = progress?.Target
                };
            })
            .ToList();
    }
}
