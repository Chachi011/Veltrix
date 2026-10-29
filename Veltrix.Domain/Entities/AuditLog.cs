namespace Veltrix.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }

    /// <summary>Who did it. Null = the system itself (e.g. the round engine).</summary>
    public int? ActorUserId { get; set; }

    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;

    /// <summary>String, because some entities have string keys (e.g. SystemSetting.Key).</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>JSON snapshot before the change.</summary>
    public string? OldValue { get; set; }

    /// <summary>JSON snapshot after the change.</summary>
    public string? NewValue { get; set; }

    public DateTime CreatedAt { get; set; }

    public User? ActorUser { get; set; }
}
