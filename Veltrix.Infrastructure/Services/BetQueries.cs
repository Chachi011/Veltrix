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

    /// <summary>The smallest bet any active game accepts (0 when no game is active). Below it, a player cannot bet at all.</summary>
    public static async Task<decimal> SmallestActiveMinBetAsync(this DbSet<Game> games, CancellationToken ct) =>
        await games.Where(g => g.Status == GameStatus.Active).MinAsync(g => (decimal?)g.MinBet, ct) ?? 0;
}
