using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Veltrix.Infrastructure.Persistence;

/// <summary>
/// Optimistic concurrency means "detect the conflict, then try again with fresh data".
/// Used for every balance change: if another request changed the same wallet (RowVersion)
/// or bet (Status) in the meantime, everything is re-read and re-checked.
/// </summary>
internal static class ConcurrencyRetry
{
    private const int MaxAttempts = 3;
    private const int SqlDeadlockVictim = 1205;

    /// <param name="action">
    /// Must load everything it needs itself (it may run up to 3 times) and open its own transaction if it needs one.
    /// </param>
    public static async Task<T> ExecuteAsync<T>(ApplicationDbContext db, Func<Task<T>> action)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsRetryable(ex))
            {
                // Forget the stale entities, so the next attempt reads the current data from the DB
                db.ChangeTracker.Clear();
            }
        }
    }

    private static bool IsRetryable(Exception ex) =>
        ex is DbUpdateConcurrencyException
        || ex.GetBaseException() is SqlException { Number: SqlDeadlockVictim };
}
