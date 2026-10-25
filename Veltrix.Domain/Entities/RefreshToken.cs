namespace Veltrix.Domain.Entities;

public class RefreshToken
{
    /// <summary>
    /// A token that was just rotated may be presented once more within this window: two tabs refreshing at
    /// the same moment, or a page reloaded before it received the new token. Outside it, reuse = theft.
    /// </summary>
    public static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

    public const string ReasonRotated = "Rotated";
    public const string ReasonLogout = "Logout";
    public const string ReasonPasswordChanged = "PasswordChanged";
    public const string ReasonReuseDetected = "ReuseDetected";

    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>SHA-256 hash of the token. The raw token is only ever sent to the client.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    /// <summary>Why it stopped working (Rotated, Logout, PasswordChanged, ReuseDetected).</summary>
    public string? RevokedReason { get; set; }

    public User? User { get; set; }

    public void Revoke(DateTime now, string reason)
    {
        RevokedAt = now;
        RevokedReason = reason;
    }

    /// <summary>
    /// True when a revoked token is presented again shortly after a normal rotation, which happens with
    /// concurrent refreshes and is not an attack. Logged-out or force-revoked tokens never qualify.
    /// </summary>
    public bool IsRecentlyRotated(DateTime now) =>
        RevokedReason == ReasonRotated
        && RevokedAt.HasValue
        && now - RevokedAt.Value <= ReuseGracePeriod
        && ExpiresAt > now;
}
