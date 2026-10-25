using Veltrix.Application.Common.Models;

namespace Veltrix.Application.DTOs.Notifications;

/// <summary>Sent by GET /api/notifications and pushed live as the SignalR "Notification" event.</summary>
public sealed class NotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class NotificationQuery : PageQuery
{
    public bool UnreadOnly { get; set; }
}

/// <summary>One page of notifications plus the number for the badge on the bell.</summary>
public sealed class NotificationPageDto : PagedResult<NotificationDto>
{
    public int UnreadCount { get; init; }
}

public sealed record MarkAllReadResultDto(int Updated);
