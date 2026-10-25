using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Admin;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

/// <summary>
/// Admin-editable settings, stored as key/value rows in SystemSettings.
/// They are read on every use (registration, daily bonus), so a change works immediately, without a restart.
/// </summary>
public sealed class SystemSettingsService : ISystemSettingsService
{
    private static readonly string[] Keys = { SettingKeys.StartingBalance, SettingKeys.DailyBonusAmount };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _time;
    private readonly ILogger<SystemSettingsService> _logger;

    public SystemSettingsService(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        TimeProvider time,
        ILogger<SystemSettingsService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _time = time;
        _logger = logger;
    }

    public async Task<SystemSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var rows = await _db.SystemSettings.AsNoTracking()
            .Where(s => Keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, ct);

        return new SystemSettingsDto
        {
            StartingBalance = Parse(rows, SettingKeys.StartingBalance, SettingDefaults.StartingBalance),
            DailyBonusAmount = Parse(rows, SettingKeys.DailyBonusAmount, SettingDefaults.DailyBonusAmount),
            UpdatedAt = rows.Count == 0 ? default : rows.Values.Max(s => s.UpdatedAt)
        };
    }

    public async Task<SystemSettingsDto> UpdateAsync(UpdateSystemSettingsRequest request, CancellationToken ct = default)
    {
        var actorId = _currentUser.UserId;
        var now = _time.GetUtcNow().UtcDateTime;

        var rows = await _db.SystemSettings
            .Where(s => Keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, ct);

        var changed = 0;
        foreach (var (key, value) in new[]
                 {
                     (SettingKeys.StartingBalance, request.StartingBalance),
                     (SettingKeys.DailyBonusAmount, request.DailyBonusAmount)
                 })
        {
            // Invariant culture: "100.5" on every server, never "100,5"
            var text = value.ToString("0.##", CultureInfo.InvariantCulture);

            if (!rows.TryGetValue(key, out var row))
            {
                row = new SystemSetting { Key = key };
                _db.SystemSettings.Add(row);
            }
            else if (row.Value == text)
            {
                continue;
            }

            // One audit row per setting, with the old and the new value
            _db.AuditLogs.Add(AuditTrail.Entry(actorId, AuditActions.SettingChanged, nameof(SystemSetting), key,
                row.Value.Length == 0 ? null : new { row.Value }, new { Value = text }, now));

            row.Value = text;
            row.UpdatedAt = now;
            changed++;
        }

        if (changed > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogWarning("Admin {ActorId} changed {Count} system setting(s)", actorId, changed);
        }

        return await GetAsync(ct);
    }

    private static decimal Parse(IReadOnlyDictionary<string, SystemSetting> rows, string key, decimal fallback) =>
        rows.TryGetValue(key, out var row)
        && decimal.TryParse(row.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
}
