namespace Veltrix.Domain.Entities;

public class Notification
{
    public const int MaxTitleLength = 150;
    public const int MaxMessageLength = 2000;

    public int Id { get; set; }
    public int UserId { get; set; }

    /// <summary>One of <see cref="Common.NotificationTypes"/>.</summary>
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }

    public static Notification Create(int userId, string type, string title, string message, DateTime now) => new()
    {
        UserId = userId,
        Type = type,
        Title = Truncate(title, MaxTitleLength),
        Message = Truncate(message, MaxMessageLength),
        CreatedAt = now
    };

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}
