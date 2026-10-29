using Microsoft.EntityFrameworkCore;
using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Admin;
using Veltrix.Application.Interfaces;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

/// <summary>Read-only view of the audit log: who changed what, and when. Rows are never edited or deleted.</summary>
public sealed class AuditLogService : IAuditLogService
{
    private const string SystemActor = "System";

    private readonly ApplicationDbContext _db;

    public AuditLogService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLogDto>> GetAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        var logs = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Action))
            logs = logs.Where(a => a.Action == query.Action);
        if (!string.IsNullOrWhiteSpace(query.EntityType))
            logs = logs.Where(a => a.EntityType == query.EntityType);
        if (!string.IsNullOrWhiteSpace(query.EntityId))
            logs = logs.Where(a => a.EntityId == query.EntityId);
        if (query.ActorUserId.HasValue)
            logs = logs.Where(a => a.ActorUserId == query.ActorUserId.Value);
        if (query.From.HasValue)
            logs = logs.Where(a => a.CreatedAt >= query.From.Value);
        if (query.To.HasValue)
            logs = logs.Where(a => a.CreatedAt <= query.To.Value);

        var total = await logs.CountAsync(ct);
        var items = await logs
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Skip(query.Offset()).Take(query.PageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                ActorUserId = a.ActorUserId,
                ActorNickname = a.ActorUser != null ? a.ActorUser.Nickname : SystemActor,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto> { Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = total };
    }
}
