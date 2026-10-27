using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Limits;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

/// <summary>Responsible play tools. The rules themselves are in the PlayerLimit entity.</summary>
public sealed class LimitsService : ILimitsService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameNotifier _notifier;
    private readonly TimeProvider _time;
    private readonly ILogger<LimitsService> _logger;

    public LimitsService(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IGameNotifier notifier,
        TimeProvider time,
        ILogger<LimitsService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
        _time = time;
        _logger = logger;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    private int CurrentUserId => _currentUser.UserId ?? throw new UnauthorizedException("Not logged in.");

    public async Task<PlayerLimitsDto> GetMyLimitsAsync(CancellationToken ct = default)
    {
        var userId = CurrentUserId;

        // Every user gets a PlayerLimit row at registration; the fallback only covers very old accounts
        var limit = await _db.PlayerLimits.AsNoTracking().FirstOrDefaultAsync(l => l.UserId == userId, ct)
            ?? new PlayerLimit { UserId = userId };

        return await ToDtoAsync(limit, ct);
    }

    public async Task<PlayerLimitsDto> UpdateMyLimitsAsync(UpdateLimitsRequest request, CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        var now = Now;
        var limit = await LoadForUpdateAsync(userId, ct);

        var before = Snapshot(limit);
        limit.ChangeDailyBetLimit(request.DailyBetLimit, now); // lower = now, higher = in 24 hours
        limit.ChangeSessionReminder(request.SessionReminderMin);
        var after = Snapshot(limit);

        // Anonymous types compare by value, so this is "did anything really change?"
        if (!before.Equals(after))
        {
            _db.AuditLogs.Add(AuditTrail.Entry(userId, AuditActions.LimitsChanged, nameof(User), userId.ToString(), before, after, now));
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("User {UserId} changed limits: {Before} -> {After}", userId, before, after);
        }

        return await ToDtoAsync(limit, ct);
    }

    public async Task<PlayerLimitsDto> StartSelfExclusionAsync(SelfExclusionRequest request, CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        var now = Now;
        var limit = await LoadForUpdateAsync(userId, ct);

        // Domain rule: can be extended, never shortened
        var exclusion = limit.StartSelfExclusion(request.Period, now);
        _db.SelfExclusions.Add(exclusion);

        _db.AuditLogs.Add(AuditTrail.Entry(userId, AuditActions.SelfExclusionStarted, nameof(User), userId.ToString(),
            null, new { Period = request.Period.ToString(), exclusion.EndsAt }, now));

        var notification = UserNotifications.Add(_db, userId, NotificationTypes.SelfExcluded,
            "Self-exclusion started",
            $"Betting is paused until {exclusion.EndsAt:yyyy-MM-dd HH:mm} UTC. You can still sign in and see your history. " +
            "A self-exclusion cannot be cancelled or shortened.",
            now);

        // Limit + history row + audit + notification: one SaveChanges = one transaction
        await _db.SaveChangesAsync(ct);
        await _notifier.PushAsync(notification);

        _logger.LogInformation("User {UserId} self-excluded for {Period} until {EndsAt}", userId, request.Period, exclusion.EndsAt);

        return await ToDtoAsync(limit, ct);
    }

    private async Task<PlayerLimit> LoadForUpdateAsync(int userId, CancellationToken ct)
    {
        var limit = await _db.PlayerLimits.FirstOrDefaultAsync(l => l.UserId == userId, ct);
        if (limit is not null)
            return limit;

        limit = new PlayerLimit { UserId = userId };
        _db.PlayerLimits.Add(limit);
        return limit;
    }

    private static object Snapshot(PlayerLimit limit) => new
    {
        limit.DailyBetLimit,
        limit.PendingDailyBetLimit,
        limit.PendingLimitAppliesAt,
        limit.SessionReminderMin
    };

    private async Task<PlayerLimitsDto> ToDtoAsync(PlayerLimit limit, CancellationToken ct)
    {
        var now = Now;
        var betToday = await _db.Bets.TotalBetTodayAsync(limit.UserId, now, ct);
        var effective = limit.EffectiveDailyBetLimit(now);
        var pending = limit.HasPendingLimitChange(now);
        var excluded = limit.IsSelfExcluded(now);

        return new PlayerLimitsDto
        {
            DailyBetLimit = effective,
            HasPendingLimitChange = pending,
            PendingDailyBetLimit = pending ? limit.PendingDailyBetLimit : null,
            PendingLimitAppliesAt = pending ? limit.PendingLimitAppliesAt : null,
            BetToday = betToday,
            RemainingToday = effective is { } max ? Math.Max(0, max - betToday) : null,
            DayResetsAt = now.Date.AddDays(1),
            SessionReminderMin = limit.SessionReminderMin,
            IsSelfExcluded = excluded,
            SelfExcludedUntil = excluded ? limit.SelfExcludedUntil : null,
            LimitIncreaseDelayHours = (int)PlayerLimit.LimitIncreaseDelay.TotalHours
        };
    }
}
