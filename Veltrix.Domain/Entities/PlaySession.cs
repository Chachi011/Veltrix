namespace Veltrix.Domain.Entities;

/// <summary>
/// A period of continuous play. Lets the server remind the player every
/// PlayerLimit.SessionReminderMin minutes ("You have been playing for 60 minutes").
/// </summary>
public class PlaySession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int RemindersSent { get; set; }

    public User? User { get; set; }
}
