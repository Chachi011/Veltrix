namespace Veltrix.Domain.Entities;

/// <summary>A moderator silenced a user in chat until MutedUntil.</summary>
public class ChatMute
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int MutedByUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime MutedUntil { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public User? MutedByUser { get; set; }
}
