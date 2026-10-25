using Microsoft.EntityFrameworkCore;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.DTOs.Notifications;
using Veltrix.Application.Interfaces;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

public sealed class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NotificationService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private int CurrentUserId => _currentUser.UserId ?? throw new UnauthorizedException("Not logged in.");

    public async Task<NotificationPageDto> GetMyAsync(NotificationQuery query, CancellationToken ct = default)
    {
        var userId = CurrentUserId;
        var mine = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);

        var unreadCount = await mine.CountAsync(n => !n.IsRead, ct);
        var filtered = query.UnreadOnly ? mine.Where(n => !n.IsRead) : mine;
        var total = query.UnreadOnly ? unreadCount : await filtered.CountAsync(ct);

        var items = await filtered
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Skip(query.Offset()).Take(query.PageSize)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(ct);

        return new NotificationPageDto
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total,
            UnreadCount = unreadCount
        };
    }

    public async Task MarkReadAsync(int id, CancellationToken ct = default)
    {
        var userId = CurrentUserId;

        // One UPDATE; the UserId condition means nobody can mark someone else's notification
        var updated = await _db.Notifications
            .Where(n => n.Id == id && n.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);

        if (updated == 0)
            throw new NotFoundException($"Notification {id} was not found.");
    }

    public Task<int> MarkAllReadAsync(CancellationToken ct = default)
    {
        var userId = CurrentUserId;

        return _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }
}
