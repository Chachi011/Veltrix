using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Notifications;
using Veltrix.Domain.Entities;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

/// <summary>
/// Bell notifications created by other services. The row is added to the SAME DbContext as the change
/// that caused it (e.g. a balance adjustment), so both are saved together or not at all.
/// The live push happens only AFTER the save: a notification is never pushed for a change that failed.
/// </summary>
internal static class UserNotifications
{
    public static Notification Add(ApplicationDbContext db, int userId, string type, string title, string message, DateTime now)
    {
        var notification = Notification.Create(userId, type, title, message, now);
        db.Notifications.Add(notification);
        return notification;
    }

    /// <summary>Call after SaveChanges, when the row has its Id.</summary>
    public static Task PushAsync(this IGameNotifier notifier, Notification notification) =>
        notifier.NotificationAsync(notification.UserId, ToDto(notification));

    public static NotificationDto ToDto(Notification notification) => new()
    {
        Id = notification.Id,
        Type = notification.Type,
        Title = notification.Title,
        Message = notification.Message,
        IsRead = notification.IsRead,
        CreatedAt = notification.CreatedAt
    };
}
