namespace Veltrix.Domain.Entities;

/// <summary>
/// Optional feature: TOTP two-factor authentication (Google Authenticator style).
/// One row per user, so UserId is also the primary key (1-to-1 relationship).
/// </summary>
public class UserTwoFactor
{
    public int UserId { get; set; }

    /// <summary>TOTP shared secret, stored encrypted.</summary>
    public string SecretKey { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EnabledAt { get; set; }

    public User? User { get; set; }
}
