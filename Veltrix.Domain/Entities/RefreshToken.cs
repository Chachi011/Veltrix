namespace Veltrix.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>SHA-256 hash of the token. The raw token is only ever sent to the client.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public User? User { get; set; }
}
