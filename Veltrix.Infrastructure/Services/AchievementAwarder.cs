using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Domain.Progress;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

/// <summary>
/// What unlocking an achievement does, in ONE place for the round settlement and for the other actions:
/// the UserAchievement row, the XP, the VLX reward through the wallet ledger (unique key "achievement:{user}:{code}",
/// so it is never paid twice) and a bell notification. The caller saves everything in its own transaction.
/// </summary>
internal static class ProgressRewards
{
    public static Notification Unlock(ApplicationDbContext db, int userId, Achievement achievement, PlayerStats stats, Wallet? wallet, DateTime now)
    {
        db.UserAchievements.Add(new UserAchievement { UserId = userId, AchievementId = achievement.Id, UnlockedAt = now });
        stats.AddXp(achievement.XpReward, now);

        if (achievement.VlxReward > 0)
        {
            if (wallet is null)
                throw new InvalidOperationException($"Achievement {achievement.Code} pays VLX, so the wallet must be loaded.");
            wallet.Credit(achievement.VlxReward, TransactionType.Bonus, $"achievement:{userId}:{achievement.Code}", now,
                $"Achievement: {achievement.Title}");
        }

        return UserNotifications.Add(db, userId, NotificationTypes.AchievementUnlocked, "Achievement unlocked",
            $"{achievement.Title}: +{achievement.XpReward} XP, +{achievement.VlxReward} VLX.", now);
    }

    /// <summary>A level-up notification when the XP reached a higher level, otherwise null.</summary>
    public static Notification? LevelUp(ApplicationDbContext db, int userId, PlayerStats stats, IReadOnlyCollection<Level> levels, DateTime now)
    {
        if (!stats.UpdateLevel(levels))
            return null;

        var level = levels.First(l => l.Number == stats.LevelNumber);
        return UserNotifications.Add(db, userId, NotificationTypes.LevelUp, "Level up",
            $"You reached level {level.Number}: {level.Title}.", now);
    }
}

/// <summary>
/// Unlocks a Learn or Safety achievement completed by an action outside a round: verifying a round, reading the rules,
/// saving a daily limit. Runs after that action was saved, in its own transaction, and pushes the bell notification
/// and the new balance live. It never fails the action itself: a second unlock (two tabs at once) is simply ignored.
/// </summary>
public sealed class AchievementAwarder
{
    private readonly ApplicationDbContext _db;
    private readonly IGameNotifier _notifier;
    private readonly TimeProvider _time;
    private readonly ILogger<AchievementAwarder> _logger;

    public AchievementAwarder(ApplicationDbContext db, IGameNotifier notifier, TimeProvider time, ILogger<AchievementAwarder> logger)
    {
        _db = db;
        _notifier = notifier;
        _time = time;
        _logger = logger;
    }

    /// <returns>True when the achievement was unlocked now; false for staff, unknown or retired codes, or one already unlocked.</returns>
    public async Task<bool> TryUnlockAsync(int userId, string code, CancellationToken ct = default)
    {
        Outcome? outcome;
        try
        {
            outcome = await ConcurrencyRetry.ExecuteAsync(_db, () => UnlockAsync(userId, code, ct));
        }
        catch (DbUpdateException ex)
        {
            // Another request unlocked it at the same moment (unique key): that one paid the reward
            _db.ChangeTracker.Clear();
            _logger.LogInformation(ex, "Achievement {Code} for user {UserId} was already being unlocked", code, userId);
            return false;
        }

        if (outcome is null)
            return false;

        foreach (var notification in outcome.Notifications)
            await _notifier.PushAsync(notification);
        if (outcome.Balance is { } balance)
            await _notifier.BalanceUpdatedAsync(userId, balance);

        _logger.LogInformation("User {UserId} unlocked {Code}", userId, code);
        return true;
    }

    private async Task<Outcome?> UnlockAsync(int userId, string code, CancellationToken ct)
    {
        var achievement = await _db.Achievements.AsNoTracking().FirstOrDefaultAsync(a => a.Code == code && a.IsActive, ct);
        if (achievement is null)
            return null;

        // Achievements belong to players: staff accounts do not play and collect nothing
        var isPlayer = await _db.Users.AnyAsync(u => u.Id == userId && u.Role == UserRole.Player, ct);
        if (!isPlayer || await _db.UserAchievements.AnyAsync(u => u.UserId == userId && u.AchievementId == achievement.Id, ct))
            return null;

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var now = _time.GetUtcNow().UtcDateTime;

        var stats = await _db.PlayerStats.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (stats is null)
        {
            stats = new PlayerStats { UserId = userId, LevelNumber = 1, UpdatedAt = now };
            _db.PlayerStats.Add(stats);
        }

        var wallet = achievement.VlxReward > 0 ? await _db.Wallets.FirstAsync(w => w.UserId == userId, ct) : null;
        var levels = await _db.Levels.AsNoTracking().ToListAsync(ct);

        var notifications = new List<Notification> { ProgressRewards.Unlock(_db, userId, achievement, stats, wallet, now) };
        if (ProgressRewards.LevelUp(_db, userId, stats, levels, now) is { } levelUp)
            notifications.Add(levelUp);

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new Outcome(notifications, wallet?.Balance);
    }

    private sealed record Outcome(IReadOnlyList<Notification> Notifications, decimal? Balance);
}
