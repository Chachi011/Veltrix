namespace Veltrix.Domain.Entities;

/// <summary>Every login try, successful or not. Used for security review and "active users" statistics.</summary>
public class LoginAttempt
{
    public int Id { get; set; }

    /// <summary>Null when the email does not belong to any account.</summary>
    public int? UserId { get; set; }

    public string Email { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool Succeeded { get; set; }

    /// <summary>E.g. "WrongPassword", "LockedOut", "Blocked", "UnknownEmail".</summary>
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}
