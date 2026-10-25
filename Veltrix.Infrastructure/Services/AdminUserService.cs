using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Veltrix.Application.Common.Exceptions;
using Veltrix.Application.Common.Interfaces;
using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Admin;
using Veltrix.Application.DTOs.Wallet;
using Veltrix.Application.Interfaces;
using Veltrix.Domain.Common;
using Veltrix.Domain.Entities;
using Veltrix.Infrastructure.Persistence;

namespace Veltrix.Infrastructure.Services;

/// <summary>
/// Admin panel: users table, role, block/unblock, balance adjustment.
/// Every change writes an AuditLog row and a notification for the affected user in the SAME transaction.
/// </summary>
public sealed class AdminUserService : IAdminUserService
{
    private const int RecentItems = 10;

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IGameNotifier _notifier;
    private readonly TimeProvider _time;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IGameNotifier notifier,
        TimeProvider time,
        ILogger<AdminUserService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _notifier = notifier;
        _time = time;
        _logger = logger;
    }

    private DateTime Now => _time.GetUtcNow().UtcDateTime;

    private int CurrentUserId => _currentUser.UserId ?? throw new UnauthorizedException("Not logged in.");

    public async Task<PagedResult<AdminUserListItemDto>> GetUsersAsync(AdminUserQuery query, CancellationToken ct = default)
    {
        var users = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // Contains() becomes a parameterized SQL query, so user input can never inject SQL
            var search = query.Search.Trim();
            users = users.Where(u => u.Email.Contains(search) || u.Nickname.Contains(search));
        }

        if (query.Role.HasValue)
            users = users.Where(u => u.Role == query.Role.Value);
        if (query.IsBlocked.HasValue)
            users = users.Where(u => u.IsBlocked == query.IsBlocked.Value);

        var total = await users.CountAsync(ct);
        var items = await users
            .OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id)
            .Skip(query.Offset()).Take(query.PageSize)
            .Select(u => new AdminUserListItemDto
            {
                Id = u.Id,
                Email = u.Email,
                Nickname = u.Nickname,
                AvatarUrl = u.AvatarUrl,
                Role = u.Role,
                IsBlocked = u.IsBlocked,
                Balance = u.Wallet != null ? u.Wallet.Balance : 0,
                TotalBets = u.Stats != null ? u.Stats.TotalBets : 0,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync(ct);

        return new PagedResult<AdminUserListItemDto> { Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = total };
    }

    public async Task<AdminUserDetailsDto> GetUserAsync(int id, CancellationToken ct = default)
    {
        var now = Now;

        var details = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new AdminUserDetailsDto
            {
                Id = u.Id,
                Email = u.Email,
                Nickname = u.Nickname,
                AvatarUrl = u.AvatarUrl,
                Role = u.Role,
                DateOfBirth = u.DateOfBirth,
                IsBlocked = u.IsBlocked,
                IsEmailVerified = u.IsEmailVerified,
                FailedLoginCount = u.FailedLoginCount,
                LockoutUntil = u.LockoutUntil > now ? u.LockoutUntil : null,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                ActiveSessions = u.RefreshTokens.Count(t => t.RevokedAt == null && t.ExpiresAt > now),
                Balance = u.Wallet != null ? u.Wallet.Balance : 0,
                TotalBets = u.Stats != null ? u.Stats.TotalBets : 0,
                CashOutCount = u.Stats != null ? u.Stats.CashOutCount : 0,
                TotalWagered = u.Stats != null ? u.Stats.TotalWagered : 0,
                TotalPayout = u.Stats != null ? u.Stats.TotalPayout : 0,
                BiggestMultiplier = u.Stats != null ? u.Stats.BiggestMultiplier : 0,
                // Same rule as PlayerLimit.EffectiveDailyBetLimit, written so SQL can evaluate it
                DailyBetLimit = u.Limit == null
                    ? null
                    : u.Limit.PendingLimitAppliesAt != null && u.Limit.PendingLimitAppliesAt <= now
                        ? u.Limit.PendingDailyBetLimit
                        : u.Limit.DailyBetLimit,
                SelfExcludedUntil = u.Limit != null && u.Limit.SelfExcludedUntil > now ? u.Limit.SelfExcludedUntil : null
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException($"User {id} was not found.");

        details.RecentTransactions = await _db.WalletTransactions
            .AsNoTracking()
            .Where(t => t.Wallet!.UserId == id)
            .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
            .Take(RecentItems)
            .Select(t => new WalletTransactionDto
            {
                Id = t.Id,
                Type = t.Type,
                Amount = t.Amount,
                BalanceBefore = t.BalanceBefore,
                BalanceAfter = t.BalanceAfter,
                BetId = t.BetId,
                Reason = t.Reason,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(ct);

        details.RecentAdjustments = await _db.BalanceAdjustments
            .AsNoTracking()
            .Where(a => a.UserId == id)
            .OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Take(RecentItems)
            .Select(a => new BalanceAdjustmentDto
            {
                Id = a.Id,
                UserId = a.UserId,
                Amount = a.Amount,
                Reason = a.Reason,
                AdminNickname = a.AdminUser!.Nickname,
                BalanceBefore = a.WalletTransaction!.BalanceBefore,
                BalanceAfter = a.WalletTransaction.BalanceAfter,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(ct);

        return details;
    }

    public async Task<AdminUserDetailsDto> ChangeRoleAsync(int id, ChangeRoleRequest request, CancellationToken ct = default)
    {
        var actorId = CurrentUserId;
        var now = Now;
        var user = await LoadUserAsync(id, ct);
        var oldRole = user.Role;

        // Domain rule: nobody changes their own role (an admin could lock everyone out of the admin panel)
        if (user.ChangeRole(request.Role, actorId))
        {
            _db.AuditLogs.Add(AuditTrail.Entry(actorId, AuditActions.UserRoleChanged, nameof(User), user.Id.ToString(),
                new { Role = oldRole.ToString() }, new { Role = user.Role.ToString() }, now));

            var notification = UserNotifications.Add(_db, user.Id, NotificationTypes.RoleChanged,
                "Your role was changed",
                $"An administrator changed your role from {oldRole} to {user.Role}.",
                now);

            await _db.SaveChangesAsync(ct);

            // The browser refreshes its token when it sees this, so the menu matches the new role at once.
            // An access token issued before the change keeps the old role until it expires (max 15 minutes).
            await _notifier.PushAsync(notification);

            _logger.LogWarning("Admin {ActorId} changed the role of user {UserId}: {Old} -> {New}", actorId, user.Id, oldRole, user.Role);
        }

        return await GetUserAsync(id, ct);
    }

    public async Task<AdminUserDetailsDto> SetBlockedAsync(int id, BlockUserRequest request, CancellationToken ct = default)
    {
        var actorId = CurrentUserId;
        var now = Now;
        var user = await LoadUserAsync(id, ct);
        var reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();

        var changed = request.IsBlocked ? user.Block(actorId) : user.Unblock();
        if (!changed)
            return await GetUserAsync(id, ct);

        _db.AuditLogs.Add(AuditTrail.Entry(actorId,
            request.IsBlocked ? AuditActions.UserBlocked : AuditActions.UserUnblocked,
            nameof(User), user.Id.ToString(),
            new { IsBlocked = !request.IsBlocked }, new { IsBlocked = request.IsBlocked, Reason = reason }, now));

        var notification = request.IsBlocked
            ? UserNotifications.Add(_db, user.Id, NotificationTypes.AccountBlocked, "Your account was blocked",
                reason is null ? "An administrator blocked your account." : $"An administrator blocked your account. Reason: {reason}", now)
            : UserNotifications.Add(_db, user.Id, NotificationTypes.AccountUnblocked, "Your account was unblocked",
                "An administrator unblocked your account. You can sign in and play again.", now);

        // The block and the logout of every device succeed or fail together
        await using (var transaction = await _db.Database.BeginTransactionAsync(ct))
        {
            await _db.SaveChangesAsync(ct);

            if (request.IsBlocked)
                await _db.RefreshTokens.RevokeAllForUserAsync(user.Id, now, RefreshToken.ReasonBlocked, ct);

            await transaction.CommitAsync(ct);
        }

        // Open tabs of the blocked player get this at once and sign out; bets are refused anyway (BetService)
        await _notifier.PushAsync(notification);

        _logger.LogWarning("Admin {ActorId} {Action} user {UserId}. Reason: {Reason}",
            actorId, request.IsBlocked ? "blocked" : "unblocked", user.Id, reason ?? "-");

        return await GetUserAsync(id, ct);
    }

    public async Task<BalanceAdjustmentDto> AdjustBalanceAsync(int id, BalanceAdjustmentRequest request, CancellationToken ct = default)
    {
        var actorId = CurrentUserId;

        // Idempotency: the same RequestId can only ever be applied once (unique index on the ledger key)
        var key = $"admin-adjust:{(request.RequestId ?? Guid.NewGuid()):N}";
        if (await _db.WalletTransactions.AnyAsync(t => t.IdempotencyKey == key, ct))
            throw new ConflictException("This adjustment was already applied.");

        var adminNickname = await _db.Users.Where(u => u.Id == actorId).Select(u => u.Nickname).FirstAsync(ct);

        var (adjustment, notification) = await ConcurrencyRetry.ExecuteAsync(_db, async () =>
        {
            var now = Now;
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == id, ct)
                ?? throw new NotFoundException($"User {id} was not found.");

            var balanceBefore = wallet.Balance;

            // Domain rules: reason required, not zero, never below zero, not your own wallet
            var newAdjustment = BalanceAdjustment.Apply(wallet, actorId, request.Amount, request.Reason, key, now);
            _db.BalanceAdjustments.Add(newAdjustment);

            _db.AuditLogs.Add(AuditTrail.Entry(actorId, AuditActions.BalanceAdjusted, nameof(User), id.ToString(),
                new { Balance = balanceBefore },
                new { Balance = wallet.Balance, newAdjustment.Amount, newAdjustment.Reason },
                now));

            var verb = newAdjustment.Amount > 0 ? "added" : "removed";
            var newNotification = UserNotifications.Add(_db, id, NotificationTypes.BalanceAdjusted, "Balance adjusted",
                $"An administrator {verb} {Math.Abs(newAdjustment.Amount):0.00} VLX. Reason: {newAdjustment.Reason}. " +
                $"New balance: {wallet.Balance:0.00} VLX.",
                now);

            // Wallet + ledger row + adjustment + audit + notification: ONE SaveChanges = one transaction.
            // If the player bet at the same moment, the wallet RowVersion makes this fail and the retry re-reads it.
            await _db.SaveChangesAsync(ct);
            return (newAdjustment, newNotification);
        });

        var transaction = adjustment.WalletTransaction!;
        _logger.LogWarning("Admin {ActorId} adjusted the balance of user {UserId} by {Amount} ({Reason}); balance {Before} -> {After}",
            actorId, id, adjustment.Amount, adjustment.Reason, transaction.BalanceBefore, transaction.BalanceAfter);

        await _notifier.BalanceUpdatedAsync(id, transaction.BalanceAfter);
        await _notifier.PushAsync(notification);

        return new BalanceAdjustmentDto
        {
            Id = adjustment.Id,
            UserId = id,
            Amount = adjustment.Amount,
            Reason = adjustment.Reason,
            AdminNickname = adminNickname,
            BalanceBefore = transaction.BalanceBefore,
            BalanceAfter = transaction.BalanceAfter,
            CreatedAt = adjustment.CreatedAt
        };
    }

    private async Task<User> LoadUserAsync(int id, CancellationToken ct) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
        ?? throw new NotFoundException($"User {id} was not found.");
}
