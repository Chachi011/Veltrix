using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veltrix.Application.DTOs.Notifications;
using Veltrix.Application.Interfaces;

namespace Veltrix.API.Controllers;

/// <summary>The bell. New notifications also arrive live as the SignalR "Notification" event.</summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>My notifications, newest first, with the unread count for the badge. unreadOnly=true hides read ones.</summary>
    [HttpGet]
    [ProducesResponseType<NotificationPageDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationPageDto>> Get([FromQuery] NotificationQuery query, CancellationToken ct) =>
        Ok(await _notificationService.GetMyAsync(query, ct));

    [HttpPut("{id:int}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        await _notificationService.MarkReadAsync(id, ct);
        return NoContent();
    }

    /// <summary>Marks all of my notifications as read. Returns how many changed.</summary>
    [HttpPut("read-all")]
    [ProducesResponseType<MarkAllReadResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<MarkAllReadResultDto>> MarkAllRead(CancellationToken ct) =>
        Ok(new MarkAllReadResultDto(await _notificationService.MarkAllReadAsync(ct)));
}
