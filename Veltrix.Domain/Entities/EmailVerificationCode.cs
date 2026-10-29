namespace Veltrix.Domain.Entities;

/// <summary>Optional feature: confirms that the user owns the email address.</summary>
public class EmailVerificationCode
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public User? User { get; set; }
}
