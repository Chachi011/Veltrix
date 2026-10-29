using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Veltrix.Infrastructure.Persistence;
using Veltrix.Infrastructure.Services;

namespace Veltrix.Infrastructure.Jobs;

/// <summary>
/// Stores one DailyStatistics row per FINISHED day (at startup, then every 10 minutes).
/// A finished day never changes again, so the admin charts read these small rows instead of
/// scanning every bet on each page load. Today is not stored: it is calculated live.
/// </summary>
public sealed class DailyStatisticsJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    /// <summary>The first run fills in history, but never more than a year.</summary>
    private const int MaxBackfillDays = 366;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _time;
    private readonly ILogger<DailyStatisticsJob> _logger;

    public DailyStatisticsJob(IServiceScopeFactory scopeFactory, TimeProvider time, ILogger<DailyStatisticsJob> logger)
    {
        _scopeFactory = scopeFactory;
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
                _logger.LogError(ex, "Daily statistics run failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = _time.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var firstUserAt = await db.Users.MinAsync(u => (DateTime?)u.CreatedAt, ct);
        if (firstUserAt is null)
            return;

        var firstDay = DateOnly.FromDateTime(firstUserAt.Value);
        if (firstDay < today.AddDays(-MaxBackfillDays))
            firstDay = today.AddDays(-MaxBackfillDays);

        var existing = await db.DailyStatistics
            .Where(d => d.Date >= firstDay && d.Date < today)
            .ToDictionaryAsync(d => d.Date, ct);

        var saved = 0;
        for (var date = firstDay; date < today; date = date.AddDays(1))
        {
            existing.TryGetValue(date, out var row);
            if (row is not null && DailyStatisticsCalculator.IsFinal(row))
                continue;

            var fresh = await DailyStatisticsCalculator.ComputeAsync(db, date, now, ct);
            if (row is null)
                db.DailyStatistics.Add(fresh);
            else
                DailyStatisticsCalculator.CopyValues(fresh, row);

            saved++;
        }

        if (saved == 0)
            return;

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Daily statistics stored for {Count} day(s)", saved);
    }
}
