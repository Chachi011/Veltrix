using Microsoft.EntityFrameworkCore;
using Veltrix.Domain.Entities;
using Veltrix.Domain.Enums;

namespace Veltrix.Infrastructure.Services;

internal static class BetQueries
{
    /// <summary>
    /// Total staked by the player today (UTC day). Refunded bets do not count: the player got that stake back.
    /// Used by the daily bet limit (BetService) and shown on the limits page (LimitsService).
    /// </summary>
    public static async Task<decimal> TotalBetTodayAsync(this DbSet<Bet> bets, int userId, DateTime now, CancellationToken ct)
    {
        var startOfDay = now.Date;
        return await bets
            .Where(b => b.UserId == userId && b.PlacedAt >= startOfDay && b.Status != BetStatus.Refunded)
            .SumAsync(b => (decimal?)b.Amount, ct) ?? 0;
    }
}
