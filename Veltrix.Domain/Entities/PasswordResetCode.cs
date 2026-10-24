namespace Veltrix.Domain.Entities;

/// <summary>One-time 6-digit code for "forgot password". Stored hashed.</summary>
public class PasswordResetCode
{
    public const int MaxAttempts = 5;

    public int Id { get; set; }
    public int UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public User? User { get; set; }

    public bool CanBeUsed(DateTime now) => UsedAt is null && ExpiresAt > now && Attempts < MaxAttempts;
}
