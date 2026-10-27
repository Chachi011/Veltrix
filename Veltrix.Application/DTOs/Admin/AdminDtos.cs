using Veltrix.Application.Common.Models;
using Veltrix.Application.DTOs.Wallet;
using Veltrix.Domain.Enums;

namespace Veltrix.Application.DTOs.Admin;

/// <summary>?search=&amp;role=&amp;isBlocked=&amp;page=&amp;pageSize=</summary>
public sealed class AdminUserQuery : PageQuery
{
    /// <summary>Part of the email or nickname.</summary>
    public string? Search { get; set; }

    public UserRole? Role { get; set; }
    public bool? IsBlocked { get; set; }
}

/// <summary>One row of the admin users table.</summary>
public sealed class AdminUserListItemDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; }
    public bool IsBlocked { get; set; }
    public decimal Balance { get; set; }
    public int TotalBets { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

/// <summary>Everything the admin drawer shows about one user.</summary>
public sealed class AdminUserDetailsDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsEmailVerified { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Devices with a valid refresh token (not revoked, not expired).</summary>
    public int ActiveSessions { get; set; }

    public decimal Balance { get; set; }

    public int TotalBets { get; set; }
    public int CashOutCount { get; set; }
    public decimal TotalWagered { get; set; }
    public decimal TotalPayout { get; set; }
    public decimal BiggestMultiplier { get; set; }

    public decimal? DailyBetLimit { get; set; }
    public DateTime? SelfExcludedUntil { get; set; }

    public IReadOnlyList<WalletTransactionDto> RecentTransactions { get; set; } = Array.Empty<WalletTransactionDto>();
    public IReadOnlyList<BalanceAdjustmentDto> RecentAdjustments { get; set; } = Array.Empty<BalanceAdjustmentDto>();
}

public sealed class ChangeRoleRequest
{
    public UserRole Role { get; set; }
}

public sealed class BlockUserRequest
{
    /// <summary>true = block, false = unblock.</summary>
    public bool IsBlocked { get; set; }

    /// <summary>Optional note, stored in the audit log.</summary>
    public string? Reason { get; set; }
}

public sealed class BalanceAdjustmentRequest
{
    /// <summary>Positive adds VLX, negative removes VLX. Never zero.</summary>
    public decimal Amount { get; set; }

    /// <summary>Required, 5-500 characters. The player sees it in their transaction history.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Optional idempotency key. The admin UI sends one per dialog, so a double-click (or a retried
    /// request) cannot apply the same adjustment twice. Leave it empty in Swagger.
    /// </summary>
    public Guid? RequestId { get; set; }
}

public sealed class BalanceAdjustmentDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string AdminNickname { get; set; } = string.Empty;
    public decimal BalanceBefore { get; set; }
    public decimal BalanceAfter { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>?action=&amp;entityType=&amp;entityId=&amp;actorUserId=&amp;from=&amp;to=&amp;page=</summary>
public sealed class AuditLogQuery : PageQuery
{
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public int? ActorUserId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public sealed class AuditLogDto
{
    public int Id { get; set; }
    public int? ActorUserId { get; set; }

    /// <summary>"System" when the server itself did it (e.g. the round engine).</summary>
    public string ActorNickname { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;

    /// <summary>JSON snapshot before the change.</summary>
    public string? OldValue { get; set; }

    /// <summary>JSON snapshot after the change.</summary>
    public string? NewValue { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>Runtime settings an Admin can change without a deployment.</summary>
public sealed class SystemSettingsDto
{
    /// <summary>VLX every NEW account starts with.</summary>
    public decimal StartingBalance { get; set; }

    /// <summary>VLX the daily bonus gives (once every 24 hours).</summary>
    public decimal DailyBonusAmount { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public sealed class UpdateSystemSettingsRequest
{
    public decimal StartingBalance { get; set; }
    public decimal DailyBonusAmount { get; set; }
}
