using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

internal static class SettingsReader
{
    public static async Task<decimal> GetDecimalAsync(
        ApplicationDbContext db, string key, decimal fallback, CancellationToken ct)
    {
        var value = await db.SystemSettings
            .AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(ct);

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : fallback;
    }
}
