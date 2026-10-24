namespace Veltrix.Domain.Entities;

public class ChatMessage
{
    public const int MaxLength = 300;

    public int Id { get; set; }
    public int UserId { get; set; }
    public string Text { get; set; } = string.Empty;

    /// <summary>Soft delete: a moderator hides the message, but it stays for review.</summary>
    public bool IsDeleted { get; set; }

    public int? DeletedByUserId { get; set; }
    public DateTime SentAt { get; set; }

    public User? User { get; set; }
    public User? DeletedByUser { get; set; }
}
