using System.Text.Json;
using Veltrix.Domain.Entities;

namespace Veltrix.Infrastructure.Services;

/// <summary>
/// Builds AuditLog rows. The caller adds the row to the same DbContext as the change itself,
/// so the change and its audit entry are saved in ONE transaction (both or neither).
/// </summary>
internal static class AuditTrail
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static AuditLog Entry(
        int? actorUserId,
        string action,
        string entityType,
        string entityId,
        object? oldValue,
        object? newValue,
        DateTime now) => new()
    {
        ActorUserId = actorUserId,
        Action = action,
        EntityType = entityType,
        EntityId = entityId,
        OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue, JsonOptions),
        NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue, JsonOptions),
        CreatedAt = now
    };
}
