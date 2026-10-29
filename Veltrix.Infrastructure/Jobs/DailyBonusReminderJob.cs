using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;
using Veltrix.Infrastructure.Persistence;
using Veltrix.Infrastructure.Services;

namespace Veltrix.Infrastructure.Jobs;

/// <summary>
/// Once a minute: finds players whose daily bonus has become available and who were not told yet,
/// creates a "Daily bonus available" notification for each and pushes it live to their open tabs.
/// </summary>
public sealed class DailyBonusReminderJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private const int BatchSize = 500;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IGameNotifier _notifier;
    private readonly TimeProvider _time;
    private readonly ILogger<DailyBonusReminderJob> _logger;

    public DailyBonusReminderJob(
        IServiceScopeFactory scopeFactory,
        IGameNotifier notifier,
        TimeProvider time,
        ILogger<DailyBonusReminderJob> logger)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failed run must not stop the job; the next tick simply tries again
                _logger.LogError(ex, "Daily bonus reminder run failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = _time.GetUtcNow().UtcDateTime;
        var availableSince = now - Wallet.DailyBonusInterval;

        var userIds = await db.Wallets
            .AsNoTracking()
            .Where(w => w.User!.Role == UserRole.Player
                        && !w.User.IsBlocked
                        // A self-excluded player is not invited back to play
                        && (w.User.Limit == null || w.User.Limit.SelfExcludedUntil == null || w.User.Limit.SelfExcludedUntil <= now)
                        && (w.LastDailyBonusAt == null || w.LastDailyBonusAt <= availableSince)
                        // Not told yet since the last claim (or ever, if never claimed)
                        && !db.Notifications.Any(n => n.UserId == w.UserId
                                                      && n.Type == NotificationTypes.DailyBonus
                                                      && (w.LastDailyBonusAt == null || n.CreatedAt > w.LastDailyBonusAt)))
            .OrderBy(w => w.UserId)
            .Select(w => w.UserId)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (userIds.Count == 0)
            return;

        var amount = await SettingsReader.GetDecimalAsync(db, SettingKeys.DailyBonusAmount, SettingDefaults.DailyBonusAmount, ct);
        var notifications = userIds
            .Select(userId => Notification.Create(userId, NotificationTypes.DailyBonus, "Daily bonus available",
                $"Your daily bonus of {amount:0.##} VLX is ready. Claim it on the wallet page.", now))
            .ToList();

        db.Notifications.AddRange(notifications);
        await db.SaveChangesAsync(ct);

        foreach (var notification in notifications)
            await _notifier.PushAsync(notification);

        _logger.LogInformation("Daily bonus reminder sent to {Count} player(s)", notifications.Count);
    }
}
