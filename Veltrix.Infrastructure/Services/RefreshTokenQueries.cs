using Microsoft.EntityFrameworkCore;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Services;

internal static class RefreshTokenQueries
{
    /// <summary>
    /// Logs the user out on every device. One UPDATE statement directly in SQL,
    /// without loading the tokens into memory first.
    /// </summary>
    public static Task<int> RevokeAllForUserAsync(
        this DbSet<RefreshToken> tokens, int userId, DateTime now, string reason, CancellationToken ct) =>
        tokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokedReason, reason), ct);
}
